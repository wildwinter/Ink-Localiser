using System.Text;

namespace InkLocaliser
{
    public class CSVHandler {

        public class Options {
            public string outputFilePath = "";
        }

        private Options _options;
        private Localiser _localiser;

        public CSVHandler(Localiser localiser, Options? options = null) {
            _localiser = localiser;
            _options = options ?? new Options();
        }

        public bool WriteStrings() {

            string outputFilePath = Path.GetFullPath(_options.outputFilePath);

            try {
                StringBuilder output = new();
                output.AppendLine("ID,Text");

                foreach(var locID in _localiser.GetStringKeys()) {
                    var textValue = _localiser.GetString(locID);
                    textValue = textValue.Replace("\"", "\"\"");
                    var line = $"{locID},\"{textValue}\"";
                    output.AppendLine(line);
                }

                string fileContents = output.ToString();
                File.WriteAllText(outputFilePath, fileContents, Encoding.UTF8);
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out CSV file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}