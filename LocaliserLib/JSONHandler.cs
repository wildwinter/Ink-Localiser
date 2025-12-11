using System.Text;
using System.Text.Json;

namespace InkLocaliser
{
    public class JSONHandler {

        public class Options {
            public string outputFilePath = "";
            // File path for exporting origins
            public string originsFilePath = "";
        }

        private Options _options;
        private Localiser _localiser;

        public JSONHandler(Localiser localiser, Options? options = null) {
            _localiser = localiser;
            _options = options ?? new Options();
        }

        public bool WriteStrings() {

            string outputFilePath = Path.GetFullPath(_options.outputFilePath);

            try {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Dictionary<string, string> entries = new();

                foreach(var locID in _localiser.GetStringKeys()) {
                    entries.Add(locID, _localiser.GetString(locID));
                }
                string fileContents = JsonSerializer.Serialize(entries, options);

                File.WriteAllText(outputFilePath, fileContents, Encoding.UTF8);
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out JSON file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }

        public bool WriteOrigins() {

            string outputFilePath = Path.GetFullPath(_options.originsFilePath);

            try {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Dictionary<string, object> entries = new();
                foreach(var originEntry in _localiser.LineOrigins) {
                    entries.Add(originEntry.Key, new {File=originEntry.Value.File, LineNumber=originEntry.Value.LineNumber});
                }
                string fileContents = JsonSerializer.Serialize(entries, options);

                File.WriteAllText(outputFilePath, fileContents, Encoding.UTF8);
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out origins JSON file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }

    }
}