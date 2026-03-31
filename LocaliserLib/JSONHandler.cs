using System.Text;
using System.Text.Json;
using SimpleVCLib;

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

                var result = VCLib.WriteTextFile(outputFilePath, fileContents, Encoding.UTF8, false);
                if (!result.Success) Console.Error.WriteLine($"Error writing out JSON file {outputFilePath}: {result.Message}");
                return result.Success;
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out JSON file {outputFilePath}: " + ex.Message);
                return false;
            }
        }

        public bool WriteOrigins() {

            string outputFilePath = Path.GetFullPath(_options.originsFilePath);

            try {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string fileContents = JsonSerializer.Serialize(_localiser.LineOrigins, options);

                var result = VCLib.WriteTextFile(outputFilePath, fileContents, Encoding.UTF8, false);
                if (!result.Success) Console.Error.WriteLine($"Error writing out origins JSON file {outputFilePath}: {result.Message}");
                return result.Success;
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out origins JSON file {outputFilePath}: " + ex.Message);
                return false;
            }
        }

    }
}