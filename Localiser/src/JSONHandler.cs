using System.Text;
using System.Text.Json;

namespace InkLocaliser
{
    public class JSONHandler {

        public class Options {
            public string outputFilePath = "strings.json";
        }

        private Options _options;
        private Localiser _localiser;

        public JSONHandler(Localiser localiser, Options? options = null) {
            _localiser = localiser;
            _options = options ?? new Options();
        }

        public bool WriteStrings() {

            string outputFilePath = Path.GetFullPath(_options.outputFilePath);

            Console.WriteLine($"Writing strings to {outputFilePath}...");

            try {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Dictionary<string, string> entries = new();

                foreach(var locID in _localiser.GetStringKeys()) {
                    entries.Add(locID, _localiser.GetString(locID));
                }
                string fileContents = JsonSerializer.Serialize(entries, options);

                File.WriteAllText(outputFilePath, fileContents, Encoding.UTF8);
                Console.WriteLine($"Written {_localiser.GetStringKeys().Count} strings.");
            }
            catch (Exception ex) {
                 Console.Error.WriteLine($"Error writing out JSON file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }

    }
}