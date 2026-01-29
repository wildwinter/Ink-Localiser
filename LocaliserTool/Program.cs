using InkLocaliser;

var options = new Localiser.Options();
var csvOptions = new CSVHandler.Options();
var jsonOptions = new JSONHandler.Options();
var poOptions = new POHandler.Options();

// ----- Simple Args -----
foreach (var arg in args)
{
    if (arg.StartsWith("--file="))
        options.file = arg.Substring(7);
    else if (arg.Equals("--retag"))
        options.retag = true;
    else if (arg.ToLower().Equals("--shortids"))
        options.shortIDs = true;
    else if (arg.StartsWith("--folder="))
        options.folder = arg.Substring(9);
    else if (arg.StartsWith("--filePattern="))
        options.filePattern = arg.Substring(14);
    else if (arg.StartsWith("--csv="))
        csvOptions.outputFilePath = arg.Substring(6);
    else if (arg.StartsWith("--json="))
        jsonOptions.outputFilePath = arg.Substring(7);
    else if (arg.StartsWith("--origins="))
        jsonOptions.originsFilePath = arg.Substring(10);
    else if (arg.StartsWith("--pot="))
        poOptions.potFilePath = arg.Substring(6);
    else if (arg.StartsWith("--po-dir="))
        poOptions.poDirectory = arg.Substring(9);
    else if (arg.StartsWith("--po-langs="))
        poOptions.targetLanguages = arg.Substring(11).Split(',', StringSplitOptions.RemoveEmptyEntries);
    else if (arg.StartsWith("--po-source-lang="))
        poOptions.sourceLanguage = arg.Substring(17);
    else {
        Console.WriteLine("Ink Localiser");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  --file=<inkFile> - File to convert, relative to working dir.");
        Console.WriteLine("                      e.g. --file=inkFile/.ink");
        Console.WriteLine("                      If this is specified, no need to supply --folder or --filePattern.");
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
        Console.WriteLine("  --origins=<jsonFile> - Path to a JSON file of line origins to export, relative to working dir.");
        Console.WriteLine("                      e.g. --origins=output/origins.json");
        Console.WriteLine("                      Default is empty, so no JSON file will be exported.");
        Console.WriteLine("  --pot=<potFile> - Path to a POT template file to export, relative to working dir.");
        Console.WriteLine("                    e.g. --pot=locale/messages.pot");
        Console.WriteLine("                    Default is empty, so no POT file will be exported.");
        Console.WriteLine("  --po-dir=<folder> - Folder for per-language PO files, relative to working dir.");
        Console.WriteLine("                      e.g. --po-dir=locale/");
        Console.WriteLine("                      Requires --po-langs to also be specified.");
        Console.WriteLine("  --po-langs=<codes> - Comma-separated target language codes for PO files.");
        Console.WriteLine("                       e.g. --po-langs=fr,de,es,ja");
        Console.WriteLine("                       Requires --po-dir to also be specified.");
        Console.WriteLine("  --po-source-lang=<code> - Source language code for PO/POT headers.");
        Console.WriteLine("                            e.g. --po-source-lang=en");
        Console.WriteLine("                            Default is 'en'.");
        Console.WriteLine("  --retag - Regenerate all localisation tag IDs, rather than keep old IDs.");
        Console.WriteLine("  --shortIDs - Use short-form IDs.");
        return 0;
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
var jsonHandler = new JSONHandler(localiser, jsonOptions);

if (!String.IsNullOrEmpty(jsonOptions.outputFilePath))
{
    if (!jsonHandler.WriteStrings()) {
        Console.Error.WriteLine("Database not written.");
        return -1;
    }
    Console.WriteLine($"JSON file written: {jsonOptions.outputFilePath}");
}

if (!String.IsNullOrEmpty(jsonOptions.originsFilePath))
{
    if (!jsonHandler.WriteOrigins()) {
        Console.Error.WriteLine("Origins not written.");
        return -1;
    }
    Console.WriteLine($"Origins file written: {jsonOptions.originsFilePath}");
}

// ----- PO/POT Output -----
var poHandler = new POHandler(localiser, poOptions);

if (!String.IsNullOrEmpty(poOptions.potFilePath))
{
    if (!poHandler.WritePOT()) {
        Console.Error.WriteLine("POT file not written.");
        return -1;
    }
    Console.WriteLine($"POT file written: {poOptions.potFilePath}");
}

if (!String.IsNullOrEmpty(poOptions.poDirectory) && poOptions.targetLanguages.Length > 0)
{
    if (!poHandler.WritePOFiles()) {
        Console.Error.WriteLine("PO files not written.");
        return -1;
    }
}

return 0;
