using InkLocaliser;
using Xunit;

namespace LocaliserLib.Tests;

// A line split into multiple text chunks by inline logic - "Test {value}
// Again" - can't be localised as a single string. In strict mode (the default)
// that is a hard failure; in lenient mode it is warned about and skipped,
// leaving the line untagged so the rest of the file still localises.
public class StrictModeTests
{
    private const string InkWithSplitLine =
        "VAR count = 5\n" +
        "A normal line.\n" +
        "Test {count} Again\n" +
        "-> END\n";

    private static string WriteInk(string contents)
    {
        var dir = TestHelpers.MakeTempDir();
        var path = Path.Combine(dir, "main.ink");
        File.WriteAllText(path, contents);
        return path;
    }

    [Fact]
    public void Strict_FailsOnASplitLine()
    {
        var path = WriteInk(InkWithSplitLine);
        var localiser = new Localiser(new Localiser.Options { file = path, strict = true });

        Assert.False(localiser.Run());

        // The file must be left untouched - nothing tagged when we bailed.
        Assert.DoesNotContain("#id:", File.ReadAllText(path));
    }

    [Fact]
    public void Lenient_SkipsTheSplitLineButTagsTheRest()
    {
        var path = WriteInk(InkWithSplitLine);
        var localiser = new Localiser(new Localiser.Options { file = path, strict = false });

        Assert.True(localiser.Run());

        var lines = File.ReadAllLines(path);
        string normal = lines.Single(l => l.Contains("A normal line."));
        string split = lines.Single(l => l.Contains("Test") && l.Contains("Again"));

        // The ordinary line gets a locale ID; the split line is left as-is.
        Assert.Contains("#id:", normal);
        Assert.DoesNotContain("#id:", split);

        // The split line's text is not offered to localisation.
        Assert.DoesNotContain(localiser.GetStringKeys(),
            key => localiser.GetString(key).Contains("Test") && localiser.GetString(key).Contains("Again"));
    }

    [Fact]
    public void Lenient_LeavesACleanFileFullyTagged()
    {
        // No split lines - lenient mode must behave exactly like strict success.
        var path = WriteInk("A first line.\nA second line.\n-> END\n");
        var localiser = new Localiser(new Localiser.Options { file = path, strict = false });

        Assert.True(localiser.Run());
        Assert.Equal(2, File.ReadAllLines(path).Count(l => l.Contains("#id:")));
    }

    // Two included files whose consecutive text chunks happen to share a line
    // number (zeta line 3, then alpha line 3) are separate lines, not one
    // split line. The check must compare the file as well as the line number.
    private static string WriteTwoFileInk()
    {
        var dir = TestHelpers.MakeTempDir();
        File.WriteAllText(Path.Combine(dir, "main.ink"),
            "INCLUDE zeta.ink\nINCLUDE alpha.ink\n-> Zed\n");
        File.WriteAllText(Path.Combine(dir, "zeta.ink"),
            "=== Zed\n\nZed one.\n-> Al\n");
        File.WriteAllText(Path.Combine(dir, "alpha.ink"),
            "=== Al\n\nAl one.\nAl two.\n-> DONE\n");
        return dir;
    }

    [Fact]
    public void Strict_SameLineNumberInDifferentFilesIsNotASplitLine()
    {
        var dir = WriteTwoFileInk();
        var localiser = new Localiser(new Localiser.Options { file = Path.Combine(dir, "main.ink"), strict = true });

        Assert.True(localiser.Run());
        Assert.Contains("#id:", File.ReadAllLines(Path.Combine(dir, "zeta.ink")).Single(l => l.Contains("Zed one.")));
        Assert.Contains("#id:", File.ReadAllLines(Path.Combine(dir, "alpha.ink")).Single(l => l.Contains("Al one.")));
    }

    [Fact]
    public void Lenient_SameLineNumberInDifferentFilesTagsBothLines()
    {
        var dir = WriteTwoFileInk();
        var localiser = new Localiser(new Localiser.Options { file = Path.Combine(dir, "main.ink"), strict = false });

        Assert.True(localiser.Run());
        Assert.Contains("#id:", File.ReadAllLines(Path.Combine(dir, "zeta.ink")).Single(l => l.Contains("Zed one.")));
        Assert.Contains("#id:", File.ReadAllLines(Path.Combine(dir, "alpha.ink")).Single(l => l.Contains("Al one.")));
        Assert.Equal(3, localiser.GetStringKeys().Count);
    }
}
