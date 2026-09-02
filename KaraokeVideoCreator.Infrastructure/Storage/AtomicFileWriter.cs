using System;
using System.IO;

namespace KaraokeVideoCreator.Infrastructure.Storage
{
    public static class AtomicFileWriter
    {
        public static void WriteAllTextAtomic(string targetFilePath, string content)
        {
            if (string.IsNullOrWhiteSpace(targetFilePath))
                throw new ArgumentException("Target file path cannot be empty.", nameof(targetFilePath));

            string directory = Path.GetDirectoryName(Path.GetFullPath(targetFilePath))!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempFilePath = targetFilePath + ".tmp";

            try
            {
                File.WriteAllText(tempFilePath, content);

                if (File.Exists(targetFilePath))
                {
                    File.Replace(tempFilePath, targetFilePath, null);
                }
                else
                {
                    File.Move(tempFilePath, targetFilePath);
                }
            }
            catch
            {
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
                throw;
            }
        }
    }
}
