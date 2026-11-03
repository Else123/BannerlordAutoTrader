using System.IO;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AutoTraderSpeed.Scenarios
{
    /// <summary>
    /// Read access to AutoTrader's own log (<c>Configs\AutoTrader.log</c>, written by
    /// AutoTraderHelpers) so a scenario can check only the lines appended while it ran.
    /// </summary>
    internal static class ModLogFile
    {
        private const string FileName = "AutoTrader.log";

        public static string Path
        {
            // EngineFilePaths.ConfigsPath.Path is only the relative "Configs"; FileHelper resolves it
            // to the absolute Documents folder exactly as AutoTraderHelpers does when it writes the log.
            get { return FileHelper.GetFileFullPath(new PlatformFilePath(EngineFilePaths.ConfigsPath, FileName)); }
        }

        /// <summary>Current length in bytes; 0 when the file does not exist.</summary>
        public static long Length()
        {
            string path = Path;
            if (!File.Exists(path))
            {
                return 0;
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return stream.Length;
            }
        }

        /// <summary>
        /// Text appended since <paramref name="offset"/> (a value from <see cref="Length"/>). The mod
        /// restarts its log on launch, so a file shorter than the offset is read from the start.
        /// </summary>
        public static string ReadSince(long offset)
        {
            string path = Path;
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(offset <= stream.Length ? offset : 0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
