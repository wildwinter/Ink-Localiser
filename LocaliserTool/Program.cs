using InkLocaliser;

var options = new Localiser.Options();
var csvOptions = new CSVHandler.Options();
var jsonOptions = new JSONHandler.Options();

// ----- Simple Args -----
foreach (var arg in args)
{
    if (arg.Equals("--retag"))
        options.retag = true;
    else if (arg.StartsWith("--folder="))
        options.folder = arg.Substring(9);
    else if (arg.StartsWith("--filePattern="))
        options.filePattern = arg.Substring(14);
    else if (arg.StartsWith("--csv="))
        csvOptions.outputFilePath = arg.Substring(6);
    else if (arg.StartsWith("--json="))
        jsonOptions.outputFilePath = arg.Substring(7);
    else if (arg.Equals("--help") || arg.Equals("-h")) {
        Console.WriteLine("Ink Localiser");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  --folder=<folder> - Root folder to scan for Ink files to localise, relative to working dir.");
        Console.WriteLine("                      e.g. --folder=inkFiles/");
        Console.WriteLine("                      Default is the current working dir.");
        Console.WriteLine("  --filePattern=<folder> - Root folder to scan for Ink files to localise.");
        Console.WriteLine("                           e.g. --filePattern=start-*.ink");
        Console.WriteLine("                           Default is *.ink");
        Console.WriteLine("  --csv=<csvFile> - Path to a CSV file to export, relative to working dir.");
        Console.WriteLine("                    e.g. --csv=output/strings.csv");
        Console.WriteLine("                    Default is empty, so no CSV file will be exported.");
        Console.WriteLine("  --json=<jsonFile> - Path to a JSON file to export, relative to working dir.");
        Console.WriteLine("                      e.g. --json=output/strings.json");
        Console.WriteLine("                      Default is empty, so no JSON file will be exported.");
        Console.WriteLine("  --retag - Regenerate all localisation tag IDs, rather than keep old IDs.");
        return 0;
    }
    else if (arg.Equals("--test")) {
        options.folder="tests";
        csvOptions.outputFilePath="tests/strings.csv";
        jsonOptions.outputFilePath="tests/strings.json";
    }
}

// ----- Parse Ink, Update Tags, Build String List -----
var localiser = new Localiser(options);
if (!localiser.Run()) {
    Console.Error.WriteLine("Not localised.");
    return -1;
}
Console.WriteLine($"Localised - found {localiser.GetStringKeys().Count} strings.");

// ----- CSV Output -----
if (!String.IsNullOrEmpty(csvOptions.outputFilePath))
{
    var csvHandler = new CSVHandler(localiser, csvOptions);
    if (!csvHandler.WriteStrings()) {
        Console.Error.WriteLine("Database not written.");
        return -1;
    }
    Console.WriteLine($"CSV file written: {csvOptions.outputFilePath}");
}

// ----- JSON Output -----
if (!String.IsNullOrEmpty(jsonOptions.outputFilePath))
{
    var jsonHandler = new JSONHandler(localiser, jsonOptions);
    if (!jsonHandler.WriteStrings()) {
        Console.Error.WriteLine("Database not written.");
        return -1;
    }
    Console.WriteLine($"JSON file written: {jsonOptions.outputFilePath}");
}

return 0;
