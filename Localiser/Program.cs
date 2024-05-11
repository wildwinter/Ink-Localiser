using Ink;
using InkLocaliser;

var localiser = new Localiser();
localiser.AddFile("tests/test.ink");
localiser.AddFile("tests/test2.ink");

if (!localiser.Run()) {
    Console.Error.WriteLine("Not localised.");
    return -1;
}

Console.WriteLine("Localised!");
return 0;
