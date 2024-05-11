using Ink;
using InkLocaliser;

bool inkErrors = false;

void OnError(string message, ErrorType type)
{
    inkErrors = true;
    Console.Error.WriteLine("Ink Parse Error: "+message);
}

static string? LoadInkFile(string filePath)
{
    // Check if the file exists
    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"Can't read ink file: {filePath}");
        return null;
    }

    // Read the entire content of the file
    try
    {
        string content = File.ReadAllText(filePath);
        return content;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error reading ink file: {ex.Message}");
        return null;
    }
}

var content = LoadInkFile("tests/test.ink");
if (content==null)
    return -1;

InkParser parser = new InkParser(content, "test.ink", OnError);

var parsedStory = parser.Parse();
if (inkErrors) {
    Console.Error.WriteLine($"Error parsing ink file.");
    return -1;
}

Console.WriteLine("Parsed ink!");

var localiser = new Localiser(parsedStory);

if (!localiser.Run()) {
    Console.Error.WriteLine("Not localised.");
    return -1;
}

Console.WriteLine("Localised!");
return 0;
