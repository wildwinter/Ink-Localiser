using Ink;
using InkLocaliser;


string filePath = "tests/test.ink";

var localiser = new Localiser();
localiser.AddFile(filePath);

if (!localiser.Run()) {
    Console.Error.WriteLine("Not localised.");
    return -1;
}

Console.WriteLine("Localised!");
return 0;
