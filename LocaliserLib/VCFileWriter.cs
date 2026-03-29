using System.Text;

namespace InkLocaliser
{
    /// <summary>
    /// Thin wrapper around file write operations.
    ///
    /// When SimpleVCLib is available (HAVE_SIMPLEVCLIB is defined at build time),
    /// writes are delegated to VCLib.WriteTextFile, which handles checkout /
    /// add-to-VC automatically for Git, Perforce, Plastic SCM, and SVN.
    ///
    /// When SimpleVCLib is absent the methods fall back to plain File I/O so
    /// the tool continues to work without VC integration.
    /// </summary>
    internal static class VCFileWriter
    {
        /// <summary>
        /// Write UTF-8 text to <paramref name="filePath"/>, checking it out of
        /// version control first if needed.  Prints a descriptive error and
        /// returns false on failure.
        /// </summary>
        public static bool WriteTextFile(string filePath, string content, Encoding? encoding = null)
        {
#if HAVE_SIMPLEVCLIB
            var result = SimpleVCLib.VCLib.WriteTextFile(filePath, content, encoding ?? Encoding.UTF8);
            if (!result.Success)
            {
                string hint = result.Status switch {
                    SimpleVCLib.VCStatus.Locked    => " The file is locked by another user.",
                    SimpleVCLib.VCStatus.OutOfDate => " Your local copy is out of date — sync / update first.",
                    _                              => ""
                };
                Console.Error.WriteLine($"Version control error writing {filePath}: {result.Message}{hint}");
                return false;
            }
            return true;
#else
            try
            {
                File.WriteAllText(filePath, content, encoding ?? Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error writing {filePath}: {ex.Message}");
                return false;
            }
#endif
        }

        /// <summary>
        /// Ensure a directory exists, creating it if needed.
        /// Directory creation is always a plain filesystem operation — version
        /// control systems do not track empty directories.
        /// </summary>
        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
