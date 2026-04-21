using System.Diagnostics;
using InkLocaliser;
using SimpleVCLib;
using Xunit;

namespace LocaliserLib.Tests;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

internal static class TestHelpers
{
    public static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static bool SvnAvailable()
    {
        static int ExitCode(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                return p.ExitCode;
            }
            catch { return -1; }
        }
        return ExitCode("svn", "--version --quiet") == 0 &&
               ExitCode("svnadmin", "--version --quiet") == 0;
    }

    /// <summary>
    /// Initialise a git repo, configure identity, and make an initial commit.
    /// </summary>
    public static void InitGitRepo(string dir)
    {
        static void Run(string args, string cwd)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = cwd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
        }
        Run("init", dir);
        Run("config user.email test@example.com", dir);
        Run("config user.name \"Test User\"", dir);
        File.WriteAllText(Path.Combine(dir, "README.md"), "test");
        Run("add README.md", dir);
        Run("commit -m init", dir);
    }

    /// <summary>
    /// Create a temp SVN repository and working copy with one initial commit.
    /// Returns the working-copy path.
    /// </summary>
    public static string InitSvnRepo()
    {
        static (int exitCode, string output) Run(string exe, string args, string? cwd = null)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                WorkingDirectory = cwd ?? Path.GetTempPath(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi)!;
            var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode, output);
        }

        var repoDir = MakeTempDir();
        var wcDir   = MakeTempDir();

        Run("svnadmin", $"create \"{repoDir}\"");
        Run("svn", $"checkout \"file://{repoDir}\" \"{wcDir}\"");

        File.WriteAllText(Path.Combine(wcDir, "initial.txt"), "initial");
        Run("svn", "add initial.txt", wcDir);
        Run("svn", "commit -m initial --username test --no-auth-cache", wcDir);

        return wcDir;
    }

    /// <summary>
    /// Copy the test ink fixture into <paramref name="dir"/> and run the Localiser
    /// on it.  Returns the populated Localiser ready for use with a handler.
    /// </summary>
    public static Localiser PrepareLocaliser(string dir)
    {
        var inkDest = Path.Combine(dir, "simple.ink");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "simple.ink"), inkDest, overwrite: true);

        var localiser = new Localiser(new Localiser.Options { file = inkDest });
        bool ok = localiser.Run();
        Assert.True(ok, "Localiser.Run() should succeed on the test ink file");
        Assert.True(localiser.GetStringKeys().Count > 0, "Test ink file should contain at least one localisable string");
        return localiser;
    }

    /// <summary>Stages all changes and creates a commit in the git repo.</summary>
    public static void CommitGitRepo(string dir)
    {
        static void Run(string args, string cwd)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = cwd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
        }
        Run("add -A", dir);
        Run("commit -m snapshot", dir);
    }

    /// <summary>Commits all pending changes in the SVN working copy.</summary>
    public static void CommitSvnRepo(string wcDir)
    {
        var psi = new ProcessStartInfo("svn", "commit -m snapshot --username test --no-auth-cache")
        {
            WorkingDirectory = wcDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
    }

    /// <summary>Returns the short git status prefix for a path (e.g. "A", "M", "??").</summary>
    public static string GitStatus(string filePath, string repoDir)
    {
        var psi = new ProcessStartInfo("git", $"status --short \"{filePath}\"")
        {
            WorkingDirectory = repoDir,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return output;
    }

    /// <summary>Returns the svn status character for a path (first char of svn status output).</summary>
    public static string SvnStatus(string filePath, string wcDir)
    {
        var psi = new ProcessStartInfo("svn", $"status \"{filePath}\"")
        {
            WorkingDirectory = wcDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return output;
    }
}

// ---------------------------------------------------------------------------
// Git integration tests
// ---------------------------------------------------------------------------

/// <summary>
/// Verifies that LocaliserLib's file-writing handlers (JSON, CSV, PO/POT) use
/// SimpleVCLib correctly: output files are written AND staged in git.
/// Also verifies that the Localiser's ink-file tag insertion is staged.
/// </summary>
[Collection("VCIntegration")]
public class GitVCIntegrationTests : IDisposable
{
    private readonly string _repoDir = TestHelpers.MakeTempDir();
    private readonly Localiser _localiser;

    public GitVCIntegrationTests()
    {
        TestHelpers.InitGitRepo(_repoDir);
        VCLib.SetProvider(new GitProvider());
        // PrepareLocaliser copies the ink file into the repo and runs Localiser,
        // which will write the tagged ink file back via VCLib (staging it in git).
        _localiser = TestHelpers.PrepareLocaliser(_repoDir);
    }

    public void Dispose()
    {
        VCLib.ClearProvider();
        if (Directory.Exists(_repoDir))
            Directory.Delete(_repoDir, recursive: true);
    }

    [Fact]
    public void InkFile_TaggedByLocaliser_IsStagedInGit()
    {
        // The ink file had no tags; Localiser.Run() wrote tags back via VCLib.
        var inkPath = Path.Combine(_repoDir, "simple.ink");
        var status = TestHelpers.GitStatus(inkPath, _repoDir);
        // "A " = new file staged, "AM" = new+modified staged; both mean it was git-added.
        Assert.StartsWith("A", status);
    }

    [Fact]
    public void JSONHandler_WriteStrings_IsStagedInGit()
    {
        var outputPath = Path.Combine(_repoDir, "strings.json");
        var handler = new JSONHandler(_localiser, new JSONHandler.Options { outputFilePath = outputPath });

        bool ok = handler.WriteStrings();

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.StartsWith("A", TestHelpers.GitStatus(outputPath, _repoDir));
    }

    [Fact]
    public void JSONHandler_WriteOrigins_IsStagedInGit()
    {
        var outputPath = Path.Combine(_repoDir, "origins.json");
        var handler = new JSONHandler(_localiser, new JSONHandler.Options { originsFilePath = outputPath });

        bool ok = handler.WriteOrigins();

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.StartsWith("A", TestHelpers.GitStatus(outputPath, _repoDir));
    }

    [Fact]
    public void CSVHandler_WriteStrings_IsStagedInGit()
    {
        var outputPath = Path.Combine(_repoDir, "strings.csv");
        var handler = new CSVHandler(_localiser, new CSVHandler.Options { outputFilePath = outputPath });

        bool ok = handler.WriteStrings();

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.StartsWith("A", TestHelpers.GitStatus(outputPath, _repoDir));
    }

    [Fact]
    public void POHandler_WritePOT_IsStagedInGit()
    {
        var outputPath = Path.Combine(_repoDir, "strings.pot");
        var handler = new POHandler(_localiser, new POHandler.Options { potFilePath = outputPath });

        bool ok = handler.WritePOT();

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.StartsWith("A", TestHelpers.GitStatus(outputPath, _repoDir));
    }

    [Fact]
    public void POHandler_WritePOFiles_AreStagedInGit()
    {
        var poDir = Path.Combine(_repoDir, "po");
        var handler = new POHandler(_localiser, new POHandler.Options
        {
            poDirectory    = poDir,
            targetLanguages = new[] { "fr", "de" },
        });

        bool ok = handler.WritePOFiles();

        Assert.True(ok);
        foreach (var lang in new[] { "fr", "de" })
        {
            var filePath = Path.Combine(poDir, $"{lang}.po");
            Assert.True(File.Exists(filePath));
            Assert.StartsWith("A", TestHelpers.GitStatus(filePath, _repoDir));
        }
    }

    [Fact]
    public void JSONHandler_WriteStrings_UnchangedContent_NotUpdatedInGit()
    {
        var outputPath = Path.Combine(_repoDir, "strings.json");
        var handler = new JSONHandler(_localiser, new JSONHandler.Options { outputFilePath = outputPath });

        // First write, then commit so the repo is clean.
        handler.WriteStrings();
        TestHelpers.CommitGitRepo(_repoDir);

        // Write the same content again.
        bool ok = handler.WriteStrings();

        Assert.True(ok);
        // Content is unchanged, so the file should not appear as modified in git.
        Assert.Equal(string.Empty, TestHelpers.GitStatus(outputPath, _repoDir));
    }

    [Fact]
    public void AutoDetect_GitRepo_WritesAndStages()
    {
        // ClearProvider so VCLib auto-detects from the file path.
        VCLib.ClearProvider();

        var outputPath = Path.Combine(_repoDir, "autodetect.json");
        var handler = new JSONHandler(_localiser, new JSONHandler.Options { outputFilePath = outputPath });

        bool ok = handler.WriteStrings();

        Assert.True(ok);
        Assert.StartsWith("A", TestHelpers.GitStatus(outputPath, _repoDir));

        VCLib.SetProvider(new GitProvider()); // Restore for Dispose.
    }
}

// ---------------------------------------------------------------------------
// SVN integration tests
// Skipped automatically when svn/svnadmin are not installed.
// ---------------------------------------------------------------------------

[Collection("VCIntegration")]
public class SvnVCIntegrationTests : IDisposable
{
    private readonly bool _available;
    private readonly string _wcDir;
    private readonly Localiser? _localiser;

    public SvnVCIntegrationTests()
    {
        _available = TestHelpers.SvnAvailable();
        if (!_available)
        {
            _wcDir = string.Empty;
            return;
        }

        _wcDir = TestHelpers.InitSvnRepo();
        VCLib.SetProvider(new SvnProvider());
        _localiser = TestHelpers.PrepareLocaliser(_wcDir);
    }

    public void Dispose()
    {
        VCLib.ClearProvider();
        if (_wcDir != string.Empty && Directory.Exists(_wcDir))
            Directory.Delete(_wcDir, recursive: true);
    }

    [Fact]
    public void InkFile_TaggedByLocaliser_IsAddedInSvn()
    {
        if (!_available) return;

        var inkPath = Path.Combine(_wcDir, "simple.ink");
        var status = TestHelpers.SvnStatus(inkPath, _wcDir);
        Assert.StartsWith("A", status);
    }

    [Fact]
    public void JSONHandler_WriteStrings_IsAddedInSvn()
    {
        if (!_available) return;

        var outputPath = Path.Combine(_wcDir, "strings.json");
        var handler = new JSONHandler(_localiser!, new JSONHandler.Options { outputFilePath = outputPath });

        bool ok = handler.WriteStrings();

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.StartsWith("A", TestHelpers.SvnStatus(outputPath, _wcDir));
    }

    [Fact]
    public void CSVHandler_WriteStrings_IsAddedInSvn()
    {
        if (!_available) return;

        var outputPath = Path.Combine(_wcDir, "strings.csv");
        var handler = new CSVHandler(_localiser!, new CSVHandler.Options { outputFilePath = outputPath });

        bool ok = handler.WriteStrings();

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.StartsWith("A", TestHelpers.SvnStatus(outputPath, _wcDir));
    }

    [Fact]
    public void POHandler_WritePOT_IsAddedInSvn()
    {
        if (!_available) return;

        var outputPath = Path.Combine(_wcDir, "strings.pot");
        var handler = new POHandler(_localiser!, new POHandler.Options { potFilePath = outputPath });

        bool ok = handler.WritePOT();

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.StartsWith("A", TestHelpers.SvnStatus(outputPath, _wcDir));
    }

    [Fact]
    public void JSONHandler_WriteStrings_UnchangedContent_NotUpdatedInSvn()
    {
        if (!_available) return;

        var outputPath = Path.Combine(_wcDir, "strings.json");
        var handler = new JSONHandler(_localiser!, new JSONHandler.Options { outputFilePath = outputPath });

        // First write, then commit so the working copy is clean.
        handler.WriteStrings();
        TestHelpers.CommitSvnRepo(_wcDir);

        // Write the same content again.
        bool ok = handler.WriteStrings();

        Assert.True(ok);
        // Content is unchanged, so the file should not appear as modified in SVN.
        Assert.Equal(string.Empty, TestHelpers.SvnStatus(outputPath, _wcDir));
    }

    [Fact]
    public void AutoDetect_SvnWorkingCopy_WritesAndAdds()
    {
        if (!_available) return;

        VCLib.ClearProvider(); // Let VCLib auto-detect SVN.

        var outputPath = Path.Combine(_wcDir, "autodetect.json");
        var handler = new JSONHandler(_localiser!, new JSONHandler.Options { outputFilePath = outputPath });

        bool ok = handler.WriteStrings();

        Assert.True(ok);
        Assert.StartsWith("A", TestHelpers.SvnStatus(outputPath, _wcDir));

        VCLib.SetProvider(new SvnProvider()); // Restore for Dispose.
    }
}
