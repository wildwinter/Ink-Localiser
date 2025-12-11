using System.Text;
using System.Text.RegularExpressions;
using Ink;
using Ink.Parsed;

namespace InkLocaliser
{
    public class Localiser {

        private static string TAG_LOC = "id:";
        private static bool DEBUG_RETAG_FILES = false;

        public class Options {
            // If true, retag everything.
            public bool retag = false;
            // Root folder. If empty, uses current working dir.
            public string folder = "";
            // Files to include. Will search subfolders of the working dir.
            public string filePattern = "*.ink";
            // Use ShortIDs?
            public bool shortIDs = false;
        }
        private Options _options;

        protected struct TagInsert {
            public Text text;
            public string locID;
        }

        public struct Origin
        {
            public string File { get; set; }
            public int LineNumber { get; set; }
            public string Knot { get; set; }
            public string Stitch { get; set; }
        }
        private Dictionary<string, Origin> _origins = new();

        private InkFileHandler _fileHandler = new InkFileHandler();
        private bool _inkParseErrors = false;
        private HashSet<string> _filesVisited = new();
        private Dictionary<string, List<TagInsert>> _filesTagsToInsert = new();
        private HashSet<string> _existingIDs = new();

        private List<string> _stringKeys = new();
        private Dictionary<string, string> _stringValues = new();
        private string _previousCWD="";
        private string _folder = "";

        public List<string> UsedInkFiles {get{return _fileHandler.UsedInkFiles;}}
        public Dictionary<string, Origin> LineOrigins {get {return _origins;}}

        public Localiser(Options? options = null) {
            _options = options ?? new Options();
        }
        public bool Run() {

            bool success = true;

            // ----- Figure out which files to include -----
            List<string> inkFiles = new();
            UsedInkFiles.Clear();

            // We'll restore this later.
            _previousCWD = Environment.CurrentDirectory;

            _folder= _options.folder;
            if (String.IsNullOrWhiteSpace(_folder))
                _folder = _previousCWD;
            _folder = System.IO.Path.GetFullPath(_folder);

            // Need this for InkParser to work properly with includes and such.
            Directory.SetCurrentDirectory(_folder);

            try {                
                DirectoryInfo dir = new DirectoryInfo(_folder);
                foreach (FileInfo file in dir.GetFiles(_options.filePattern, SearchOption.AllDirectories))
                {
                    inkFiles.Add(file.FullName);
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error finding files to process: {_folder}: " + ex.Message);
                success=false;
            }

            _origins.Clear();

            // ----- For each file... -----
            if (success) {
                foreach(var inkFile in inkFiles) {
                    
                    var content = _fileHandler.LoadInkFileContents(inkFile);
                    if (content==null) {
                        success = false;
                        break;
                    }

                    InkParser parser = new InkParser(content, inkFile, OnError, _fileHandler);

                    var story = parser.Parse();
                    if (_inkParseErrors) {
                        Console.Error.WriteLine($"Error parsing ink file.");
                        success = false;
                        break;
                    }

                    // Go through the parsed story extracting existing localised lines, and lines still to be localised...
                    if (!ProcessStory(story)) {
                        success=false;
                        break;
                    }
                }
            }

            // If new tags need to be added, add them now.
            if (success) {
                if (!InsertTagsToFiles()) {
                    success=false;
                }
            }

            // Restore current directory.
            Directory.SetCurrentDirectory(_previousCWD);

            return success;
        }

        // List all the locIDs for every string we found, in order.
        public IList<string> GetStringKeys() {
            return _stringKeys;
        }

        // Return the text of a string, by locID
        public string GetString(string locID) {
            return _stringValues[locID];
        }

        private bool ProcessStory(Story story) {
                
            HashSet<string> newFilesVisited = new();

            // ---- Find all the things we should localise ----
            List<Text> validTextObjects = new List<Text>();
            int lastLineNumber = -1;
            foreach(var text in story.FindAll<Text>())
            {
                // Just a newline? Ignore.
                if (text.text.Trim()=="")
                    continue;
                
                // If it's a tag, ignore.
                if (IsTextTag(text))
                    continue;

                // Is this inside some code? In which case we can't do anything with that.
                if (text.parent is VariableAssignment ||
                    text.parent is StringExpression) {
                    continue;
                }

                // Have we already visited this source file i.e. is it in an include we've seen before?
                // If so, skip.
                string fileID = System.IO.Path.GetFileNameWithoutExtension(text.debugMetadata.fileName);
                if (_filesVisited.Contains(fileID)) {
                    continue;
                }
                newFilesVisited.Add(fileID);

                // More than one text chunk on a line? We only deal with individual lines of stuff.
                if (lastLineNumber == text.debugMetadata.startLineNumber) {
                    Console.Error.WriteLine($"Error in file {fileID} line {lastLineNumber} - two chunks of text when localiser can only work with one per line.");
                    return false;
                }
                lastLineNumber = text.debugMetadata.startLineNumber;

                validTextObjects.Add(text);
            }

            if (newFilesVisited.Count>0)
                _filesVisited.UnionWith(newFilesVisited);

            // ---- Scan for existing IDs ----
            // Build list of existing IDs (so we don't duplicate)
            if (!_options.retag) { // Don't do this if we want to retag everything.
                foreach(var text in validTextObjects) {
                    string? locTag = FindLocTagID(text);
                    if (locTag!=null)
                        _existingIDs.Add(locTag);
                }
            }

            // ---- Sort out IDs ----
            // Now we've got our list of text, let's iterate through looking for IDs, and create them when they're missing.
            // IDs are stored as tags in the form #id:file_knot_stitch_xxxx

            foreach(var text in validTextObjects) {

                string fileName = text.debugMetadata.fileName;
                string fileID = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string pathPrefix = fileID+"_";  
                string locPrefix = pathPrefix+MakeLocPrefix(text, out string knot, out string stitch);

                var origin = new Origin{
                    File = text.debugMetadata.fileName, 
                    LineNumber = text.debugMetadata.startLineNumber,
                    Knot=knot,
                    Stitch=stitch
                    };
                
                // Does the source already have a #id: tag?
                string? locID = FindLocTagID(text);

                // Skip if there's a tag and we aren't forcing a retag 
                if (locID!=null && !_options.retag) {
                    // Add existing string to localisation strings.
                    AddString(locID, text.text);
                    AddOrigin(locID,origin);
                    continue;
                }

                // Generate a new ID
                locID = GenerateUniqueID(locPrefix); 

                // Add the ID and text object to a list of things to fix up in this file.
                if (!_filesTagsToInsert.ContainsKey(fileName))
                    _filesTagsToInsert[fileName] = new List<TagInsert>();
                var insert = new TagInsert
                {
                    text = text,
                    locID = locID
                };
                _filesTagsToInsert[fileName].Add(insert);

                 // Add new string to localisation strings.
                AddString(locID, text.text);
                AddOrigin(locID, origin);
            }

            return true;
        }

        private void AddString(string locID, string value) {

            if (_stringKeys.Contains(locID)) {
                Console.Error.WriteLine($"Unexpected behaviour - trying to add content for a string named {locID}, but one already exists? Have you duplicated a tag?");
                return;
            }
            
            // Keeping the order of strings.
            _stringKeys.Add(locID);
            _stringValues[locID]=value.Trim();
        }

        private void AddOrigin(string locID, Origin origin)
        {
            // Make the origin local
            origin.File = System.IO.Path.GetRelativePath(_folder, origin.File);
            _origins[locID] = origin;
        }

        // Go through every Ink file that needs a tag insertion, and insert!
        private bool InsertTagsToFiles() {

            foreach (var (fileName, workList) in _filesTagsToInsert) {
                if (workList.Count==0)
                    continue;
                
                Console.WriteLine($"Updating IDs in file: {fileName}");

                if (!InsertTagsToFile(fileName, workList))
                    return false;
            }
            return true;
        }

        // Do the tag inserts for one specific file.
        private bool InsertTagsToFile(string fileName, List<TagInsert> workList) {

            try {             
                string filePath = _fileHandler.ResolveInkFilename(fileName);
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

                foreach(var item in workList) {

                    // Tag
                    string newTag = $"#{TAG_LOC}{item.locID}";

                    // Find out where we're supposed to do the insert.
                    int lineNumber = item.text.debugMetadata.endLineNumber-1;
                    string oldLine = lines[lineNumber];
                    string newLine = "";

                    if (oldLine.Contains($"#{TAG_LOC}")) {
                        // Is there already a tag called #id: in there? In which case, we just want to replace that.

                        // Regex pattern to find "#id:" followed by any alphanumeric characters or underscores
                        string pattern = $@"(#{TAG_LOC})\w+";

                        // Replace the matched text
                        newLine = Regex.Replace(oldLine, pattern, $"{newTag}");
                    }
                    else
                    {
                        // No tag, add a new one.
                        int charPos = item.text.debugMetadata.endCharacterNumber-1;

                        // Pad between other tags or previous item
                        if (!Char.IsWhiteSpace(oldLine[charPos-1]))
                            newTag = " "+newTag;
                        if (oldLine.Length>charPos && (oldLine[charPos]=='#' || oldLine[charPos]=='/'))
                            newTag = newTag+" ";

                        newLine = oldLine.Insert(charPos, newTag);
                    }
                    
                    lines[lineNumber] = newLine;
                }

                // Write out to the input file.
                string output = String.Join("\n", lines);
                string outputFilePath = filePath;
                if (DEBUG_RETAG_FILES)   // Debug purposes, copy to a different file instead.
                    outputFilePath += ".txt";
                File.WriteAllText(outputFilePath, output, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error replacing tags in {fileName}: " + ex.Message);
                return false;
            }
        }

        // Checking it's a tag. Is there a StartTag earlier in the parent content?        
        private bool IsTextTag(Text text) {

            int inTag = 0;
            foreach (var sibling in text.parent.content) {
                if (sibling==text)
                    break;
                if (sibling is Tag) {
                    var tag = (Tag)sibling;
                    if (tag.isStart)
                        inTag++;
                    else
                        inTag--;
                }
            }

            return (inTag>0);
        }

        private string? FindLocTagID(Text text) {
            List<string> tags = GetTagsAfterText(text);
            if (tags.Count>0) {
                foreach(var tag in tags) {
                    if (tag.StartsWith(TAG_LOC)) {
                        return tag.Substring(TAG_LOC.Length);
                    }
                }
            }
            return null;
        }

        private List<string> GetTagsAfterText(Text text) {
        
            var tags = new List<string>();

            bool afterText = false;
            int inTag = 0;

            foreach (var sibling in text.parent.content) {
                
                // Have we hit the text we care about yet? If not, carry on.
                if (sibling==text) {
                    afterText = true;
                    continue;
                }
                if (!afterText)
                    continue;

                // Have we hit an end-of-line marker? If so, stop looking, no tags here.   
                if (sibling is Text && ((Text)sibling).text=="\n")
                    break;

                // Have we found the start or end of a tag?
                if (sibling is Tag) {
                    var tag = (Tag)sibling;
                    if (tag.isStart)
                        inTag++;
                    else
                        inTag--;
                    continue;
                }

                // Have we hit the end of a tag? Add it to our tag list!
                if ((inTag>0) && (sibling is Text)) {
                    tags.Add(((Text)sibling).text.Trim());
                } 
            }
            return tags;
        }

        // Constructs a prefix from knot / stitch
        private string MakeLocPrefix(Text text, out string knot, out string stitch) {

            string prefix = "";
            knot = "";
            stitch = "";
            foreach (var obj in text.ancestry) {
                if (obj is Knot)
                {
                    knot = ((Knot)obj).name;
                    prefix+=knot+"_";
                }
                if (obj is Stitch) {
                    stitch = ((Stitch)obj).name;
                    prefix+=stitch+"_";
                }
            }

            return prefix;
        }

        private string GenerateUniqueID(string locPrefix){
            // Repeat a lot to try and get options. Should be hard to fail at this but
            // let's set a limit to stop locks.
            if (_options.shortIDs)
                locPrefix = "";
            for (int i=0;i<100;i++) {
                string locID = locPrefix+GenerateID();
                if (!_existingIDs.Contains(locID)) {
                    _existingIDs.Add(locID);
                    return locID;
                }
            }
            throw new Exception("Couldn't generate a unique ID! Really unlikely. Try again!");
        }

        private static Random _random = new Random();
        private static string GenerateID(int length=4)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] stringChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[_random.Next(chars.Length)];
            }
            return new String(stringChars);
        }

        void OnError(string message, ErrorType type)
        {
            _inkParseErrors = true;
            Console.Error.WriteLine("Ink Parse Error: "+message);
        }

        public class InkFileHandler : Ink.IFileHandler
        {
            public List<string> UsedInkFiles = new List<string>();

            public InkFileHandler()
            {
            }

            public string ResolveInkFilename(string includeName)
            {
                var workingDir = Directory.GetCurrentDirectory();
                var fullRootInkPath = System.IO.Path.Combine(workingDir, includeName);
                return fullRootInkPath;
            }

            public string LoadInkFileContents(string fullFilename)
            {
                if (!UsedInkFiles.Contains(fullFilename))
                    UsedInkFiles.Add(fullFilename);
                return File.ReadAllText(fullFilename);
            }
        }
    }
}