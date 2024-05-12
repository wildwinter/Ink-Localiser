using InkLocaliser;

var options = new Localiser.Options();
//options.retagAll = true;

var localiser = new Localiser(options);
localiser.AddFile("tests/test.ink");
localiser.AddFile("tests/test2.ink");

if (!localiser.Run()) {
    Console.Error.WriteLine("Not localised.");
    return -1;
}

var csvOptions = new CSVHandler.Options();

var csvHandler = new CSVHandler(csvOptions);
if (!csvHandler.WriteStrings(localiser)) {
    Console.Error.WriteLine("Database not written.");
    return -1;
}

Console.WriteLine("Localised!");
return 0;
