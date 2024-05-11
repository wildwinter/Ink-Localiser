using System.Collections.Specialized;
using Ink;
using Ink.Parsed;

namespace InkLocaliser
{
    public class Localiser {
        private HashSet<string> _inkFiles = new();
        private IFileHandler _fileHandler = new DefaultFileHandler();
        private bool _inkParseErrors = false;
        private List<string> _filesVisited = new();
        private OrderedDictionary _strings = new();

        public Localiser() {
        }

        public void AddFile(string inkFile) {
            _inkFiles.Add(inkFile);
        }

        public bool Run() {
            foreach(var inkFile in _inkFiles) {
                
                var content = _fileHandler.LoadInkFileContents(inkFile);
                if (content==null)
                    return false;

                InkParser parser = new InkParser(content, System.IO.Path.GetFileNameWithoutExtension(inkFile), OnError, _fileHandler);

                var story = parser.Parse();
                if (_inkParseErrors) {
                    Console.Error.WriteLine($"Error parsing ink file.");
                    return false;
                }

                if (!ProcessStory(story))
                    return false;
            }
            return true;
        }

        private bool ProcessStory(Story story) {
                
            HashSet<string> newFileIDs = new();

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

                // Is this inside a variable assignment? In which case we can't do anything with that.
                if (text.parent is VariableAssignment) {
                    continue;
                }

                // More than one tect chunk on a line? We only deal with individual lines of stuff.
                if (lastLineNumber == text.debugMetadata.startLineNumber) {
                    Console.Error.WriteLine($"Error in line {lastLineNumber} - two chunks of text when localiser can only work with one per line.");
                    return false;
                }
                lastLineNumber = text.debugMetadata.startLineNumber;

                // Have we already visited this source file i.e. is it in an include we've seen before?
                // If so, skip.
                string fileID = System.IO.Path.GetFileNameWithoutExtension(text.debugMetadata.fileName);
                if (_filesVisited.Contains(fileID)) {
                    continue;
                }
                newFileIDs.Add(fileID);
                    
                validTextObjects.Add(text);
            }

            if (newFileIDs.Count>0)
                _filesVisited.AddRange(newFileIDs);

            // ---- Sort out IDs ----
            // Now we've got our list of text, let's iterate through looking for IDs, and create them when they're missing.
            // IDs are stored as tags in the form #loc:file_knot_stitch_xxxx

            foreach(var text in validTextObjects) {

                string pathPrefix = System.IO.Path.GetFileNameWithoutExtension(text.debugMetadata.fileName)+"_";  
                string locPrefix = MakeLocPrefix(text);
                string uid = GenerateID();  
                string locID = pathPrefix+locPrefix+uid;

                Console.WriteLine("["+text.debugMetadata.startLineNumber+"] "+text.text+" : "+locID);

                string? locTag = null;
                List<string> tags = GetTagsAfterText(text);
                if (tags.Count>0) {
                    foreach(var tag in tags) {
                        if (tag.StartsWith("loc:")) {
                            locTag = tag;
                            break;
                        }
                    }
                    if (locTag!=null)
                        Console.WriteLine("  "+locTag);
                }

                _strings[locID] = text.text;
            }

            return true;
        }


        // Checking it's a tag. Is there a StartTag earlier in the parent content?        
        private bool IsTextTag(Text text) {

            int inTag = 0;

            foreach (var sibling in text.parent.content) {
                if (sibling==text) {
                    break;
                }
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

        private List<string> GetTagsAfterText(Text text) {
        
            var tags = new List<string>();

            bool afterText = false;
            int inTag = 0;

            foreach (var sibling in text.parent.content) {
                
                if (sibling==text) {
                    afterText = true;
                    continue;
                }
                if (!afterText)
                    continue;
                
                if (sibling is Tag) {
                    var tag = (Tag)sibling;
                    if (tag.isStart)
                        inTag++;
                    else
                        inTag--;
                    continue;
                }

                if ((inTag>0) && (sibling is Text)) {
                    tags.Add(((Text)sibling).text);
                } 
            }
            return tags;
        }

        // Constructs a prefix from knot / stitch
        private string MakeLocPrefix(Text text) {

            string prefix = "";
            foreach (var obj in text.ancestry) {
                if (obj is Knot)
                    prefix+=((Knot)obj).name+"_";
                if (obj is Stitch)
                    prefix+=((Stitch)obj).name+"_";
            }

            return prefix;
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
    }
}