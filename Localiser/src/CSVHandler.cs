using System.Text;

namespace InkLocaliser
{
    public class CSVHandler {


        public class Options {

            public string? outputFolder = null;
            public string? outputFileName = "strings.csv";
        }

        private Options _options;

        public CSVHandler(Options? options = null) {
            _options = options ?? new Options();
        }

        public bool WriteStrings(Localiser localiser) {

            string? outputFilePath = _options.outputFolder;
            if (_options.outputFolder==null)
                outputFilePath = Environment.CurrentDirectory;
            outputFilePath+="/"+_options.outputFileName;
            outputFilePath = Path.GetFullPath(outputFilePath);

            Console.WriteLine($"Writing strings to {outputFilePath}...");

            try {
                StringBuilder output = new();
                output.AppendLine("ID,Text");
                foreach(var locID in localiser.GetStringKeys()) {
                    var textValue = localiser.GetString(locID);
                    textValue = textValue.Replace("\"", "\\\"");
                    output.AppendLine($"{locID},\"{textValue}\"");
                }

                string fileContents = output.ToString();
                File.WriteAllText(outputFilePath, fileContents, Encoding.UTF8);
                Console.WriteLine($"Written {localiser.GetStringKeys().Count} strings");
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out CSV file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}