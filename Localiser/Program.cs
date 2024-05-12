using InkLocaliser;

var options = new Localiser.Options();
//options.retagAll = true;
//options.debugRetagFiles = false;

var localiser = new Localiser(options);
localiser.AddFile("tests/test.ink");
localiser.AddFile("tests/test2.ink");

if (!localiser.Run()) {
    Console.Error.WriteLine("Not localised.");
    return -1;
}

var csvOptions = new CSVHandler.Options();
csvOptions.outputFilePath = "tests/strings.csv";

var csvHandler = new CSVHandler(localiser, csvOptions);
if (!csvHandler.WriteStrings()) {
    Console.Error.WriteLine("Database not written.");
    return -1;
}

var jsonOptions = new JSONHandler.Options();
jsonOptions.outputFilePath = "tests/strings.json";

var jsonHandler = new JSONHandler(localiser, jsonOptions);
if (!jsonHandler.WriteStrings()) {
    Console.Error.WriteLine("Database not written.");
    return -1;
}

Console.WriteLine("Localised!");
return 0;
