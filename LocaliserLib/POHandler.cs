using System.Text;

namespace InkLocaliser
{
    public class POHandler {

        public class Options {
            public string potFilePath = "";
            public string poDirectory = "";
            public string[] targetLanguages = Array.Empty<string>();
            public string sourceLanguage = "en";
        }

        private struct POEntry {
            public string MsgCtxt;
            public string MsgId;
            public string MsgStr;
        }

        private Options _options;
        private Localiser _localiser;

        public POHandler(Localiser localiser, Options? options = null) {
            _localiser = localiser;
            _options = options ?? new Options();
        }

        // ----- Public API -----

        public bool WritePOT() {
            string outputFilePath = Path.GetFullPath(_options.potFilePath);

            try {
                string? dir = Path.GetDirectoryName(outputFilePath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                StringBuilder output = new();
                WritePOHeader(output, _options.sourceLanguage, isTemplate: true);

                foreach (var locID in _localiser.GetStringKeys()) {
                    var sourceText = _localiser.GetString(locID);
                    var origin = _localiser.LineOrigins.GetValueOrDefault(locID);
                    WritePOEntry(output, locID, sourceText, "", origin);
                }

                File.WriteAllText(outputFilePath, output.ToString(), Encoding.UTF8);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Error writing POT file {outputFilePath}: " + ex.Message);
                return false;
            }
            return true;
        }

        public bool WritePOFiles() {
            foreach (var lang in _options.targetLanguages) {
                string filePath = Path.GetFullPath(Path.Combine(_options.poDirectory, $"{lang}.po"));

                try {
                    string? dir = Path.GetDirectoryName(filePath);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    // Parse existing PO file if it exists, to preserve translations.
                    Dictionary<string, POEntry> existingEntries = new();
                    if (File.Exists(filePath)) {
                        existingEntries = ParsePOFile(filePath);
                    }

                    StringBuilder output = new();
                    WritePOHeader(output, lang, isTemplate: false);

                    // Track which existing entries we've used, so we can mark the rest obsolete.
                    HashSet<string> usedCtxts = new();

                    foreach (var locID in _localiser.GetStringKeys()) {
                        var sourceText = _localiser.GetString(locID);
                        var origin = _localiser.LineOrigins.GetValueOrDefault(locID);

                        // Preserve existing translation if available.
                        string translation = "";
                        if (existingEntries.TryGetValue(locID, out var existing)) {
                            translation = existing.MsgStr;
                            usedCtxts.Add(locID);
                        }

                        WritePOEntry(output, locID, sourceText, translation, origin);
                    }

                    // Write obsolete entries for strings that no longer exist.
                    foreach (var (ctxt, entry) in existingEntries) {
                        if (!usedCtxts.Contains(ctxt)) {
                            WriteObsoleteEntry(output, entry);
                        }
                    }

                    File.WriteAllText(filePath, output.ToString(), Encoding.UTF8);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine($"Error writing PO file {filePath}: " + ex.Message);
                    return false;
                }

                Console.WriteLine($"PO file written: {filePath}");
            }
            return true;
        }

        // ----- Private Helpers -----

        private void WritePOHeader(StringBuilder sb, string language, bool isTemplate) {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mmzzz");

            sb.AppendLine("msgid \"\"");
            sb.AppendLine("msgstr \"\"");
            sb.AppendLine($"\"Content-Type: text/plain; charset=UTF-8\\n\"");
            sb.AppendLine($"\"Content-Transfer-Encoding: 8bit\\n\"");
            sb.AppendLine($"\"MIME-Version: 1.0\\n\"");
            sb.AppendLine($"\"POT-Creation-Date: {now}\\n\"");
            if (!isTemplate) {
                sb.AppendLine($"\"Language: {language}\\n\"");
                sb.AppendLine($"\"PO-Revision-Date: {now}\\n\"");
            }
            sb.AppendLine();
        }

        private void WritePOEntry(StringBuilder sb, string locID, string sourceText,
                                   string translation, Localiser.Origin origin) {
            // Extracted comment: origin info
            if (!string.IsNullOrEmpty(origin.File)) {
                string originDesc = origin.File;
                if (!string.IsNullOrEmpty(origin.Knot)) {
                    originDesc += ", " + origin.Knot;
                    if (!string.IsNullOrEmpty(origin.Stitch))
                        originDesc += "." + origin.Stitch;
                }
                sb.AppendLine($"#. Origin: {originDesc}");
                sb.AppendLine($"#: {origin.File}:{origin.LineNumber}");
            }

            sb.AppendLine($"msgctxt {FormatPOString(locID)}");
            sb.AppendLine($"msgid {FormatPOString(sourceText)}");
            sb.AppendLine($"msgstr {FormatPOString(translation)}");
            sb.AppendLine();
        }

        private static void WriteObsoleteEntry(StringBuilder sb, POEntry entry) {
            sb.AppendLine($"#~ msgctxt {FormatPOString(entry.MsgCtxt)}");
            sb.AppendLine($"#~ msgid {FormatPOString(entry.MsgId)}");
            sb.AppendLine($"#~ msgstr {FormatPOString(entry.MsgStr)}");
            sb.AppendLine();
        }

        private static string FormatPOString(string value) {
            // Escape special characters for PO format.
            value = value.Replace("\\", "\\\\");
            value = value.Replace("\"", "\\\"");
            value = value.Replace("\n", "\\n");
            value = value.Replace("\t", "\\t");
            return $"\"{value}\"";
        }

        private static string UnescapePOString(string value) {
            // Reverse the PO escaping.
            value = value.Replace("\\t", "\t");
            value = value.Replace("\\n", "\n");
            value = value.Replace("\\\"", "\"");
            value = value.Replace("\\\\", "\\");
            return value;
        }

        /// <summary>
        /// Parse an existing PO file, returning entries keyed by msgctxt value.
        /// Skips the header entry (which has an empty msgid).
        /// </summary>
        private static Dictionary<string, POEntry> ParsePOFile(string filePath) {
            var entries = new Dictionary<string, POEntry>();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            string? currentCtxt = null;
            string? currentId = null;
            string? currentStr = null;
            bool isObsolete = false;
            // Tracks which field continuation lines belong to.
            // 0=none, 1=msgctxt, 2=msgid, 3=msgstr
            int lastField = 0;

            void CommitEntry() {
                if (currentId != null && currentCtxt != null && !string.IsNullOrEmpty(currentId)) {
                    entries[currentCtxt] = new POEntry {
                        MsgCtxt = currentCtxt,
                        MsgId = currentId,
                        MsgStr = currentStr ?? ""
                    };
                }
                currentCtxt = null;
                currentId = null;
                currentStr = null;
                isObsolete = false;
                lastField = 0;
            }

            foreach (var rawLine in lines) {
                string line = rawLine.TrimEnd();

                // Blank line = entry boundary.
                if (string.IsNullOrWhiteSpace(line)) {
                    CommitEntry();
                    continue;
                }

                // Strip obsolete prefix for parsing purposes.
                string parseLine = line;
                if (line.StartsWith("#~")) {
                    isObsolete = true;
                    parseLine = line.Substring(2).TrimStart();
                }

                // Skip comment lines (but not obsolete entries).
                if (!isObsolete && line.StartsWith("#")) {
                    continue;
                }

                if (parseLine.StartsWith("msgctxt ")) {
                    currentCtxt = ExtractQuotedString(parseLine.Substring(8));
                    lastField = 1;
                }
                else if (parseLine.StartsWith("msgid ")) {
                    currentId = ExtractQuotedString(parseLine.Substring(6));
                    lastField = 2;
                }
                else if (parseLine.StartsWith("msgstr ")) {
                    currentStr = ExtractQuotedString(parseLine.Substring(7));
                    lastField = 3;
                }
                else if (parseLine.StartsWith("\"") && parseLine.EndsWith("\"")) {
                    // Continuation line for the previous field.
                    string continued = ExtractQuotedString(parseLine);
                    switch (lastField) {
                        case 1: currentCtxt += continued; break;
                        case 2: currentId += continued; break;
                        case 3: currentStr += continued; break;
                    }
                }
            }

            // Commit final entry if file doesn't end with a blank line.
            CommitEntry();

            return entries;
        }

        /// <summary>
        /// Extract the content of a quoted PO string, unescaping special characters.
        /// Input should be like: "some text here"
        /// </summary>
        private static string ExtractQuotedString(string quoted) {
            quoted = quoted.Trim();
            if (quoted.Length >= 2 && quoted[0] == '"' && quoted[^1] == '"') {
                return UnescapePOString(quoted.Substring(1, quoted.Length - 2));
            }
            return quoted;
        }
    }
}
