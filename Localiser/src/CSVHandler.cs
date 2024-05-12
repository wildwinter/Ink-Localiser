using System.Text;

namespace InkLocaliser
{
    public class CSVHandler {

        public class Options {

            public string? outputFolder = null;
            public string? outputFileName = "strings.csv";
        }

        private Options _options;
        private Localiser _localiser;

        private string? _oldHeader=null;
        private List<string> _oldKeys = new();
        private Dictionary<string, string?> _oldRest = new();

        public CSVHandler(Localiser localiser, Options? options = null) {
            _localiser = localiser;
            _options = options ?? new Options();
        }


        public bool WriteStrings() {

            string? outputFilePath = _options.outputFolder;
            if (_options.outputFolder==null)
                outputFilePath = Environment.CurrentDirectory;
            outputFilePath+="/"+_options.outputFileName;
            outputFilePath = Path.GetFullPath(outputFilePath);

            ReadExistingStrings(outputFilePath);

            Console.WriteLine($"Writing strings to {outputFilePath}...");

            try {
                StringBuilder output = new();
                if (_oldHeader!=null)
                    output.AppendLine(_oldHeader);
                else
                    output.AppendLine("ID,Text");

                foreach(var locID in _localiser.GetStringKeys()) {
                    var textValue = _localiser.GetString(locID);
                    textValue = textValue.Replace("\"", "\"\"");
                    var newLine = $"{locID},\"{textValue}\"";
                    if (_oldRest.TryGetValue(locID, out var rest)) {
                        if (rest!=null)
                            newLine+=","+_oldRest[locID];
                    }
                    output.AppendLine(newLine);
                }

                string fileContents = output.ToString();
                File.WriteAllText(outputFilePath, fileContents, Encoding.UTF8);
                Console.WriteLine($"Written {_localiser.GetStringKeys().Count} strings");
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out CSV file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }

        private void ReadExistingStrings(string filePath) {
            _oldKeys.Clear();
            _oldRest.Clear();

            if (!Path.Exists(filePath))
                return;

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length<=1)
                return;

            _oldHeader = lines[0];

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var (locID, textAndRest) = SplitToNextField(line);
                if (textAndRest == null)
                    continue;
                var (text, rest) = SplitToNextField(textAndRest);
                _oldRest[locID] = rest;
                continue;
            }
        }

        private (string, string?) SplitToNextField(string line) {

            if (line.Length==0)
                return ("", null);

            bool inQuotes=false;

            for(var i=0;i<line.Length;i++){
                if (line[i]=='"') {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (inQuotes)
                    continue;

                if (line[i]==',') {
                    var part1 = line.Substring(0,i);
                    if (i<line.Length-1)
                        return (part1, line.Substring(i+1));
                    return (part1, null);
                }
            }

            return (line, null);
        }
    }
}