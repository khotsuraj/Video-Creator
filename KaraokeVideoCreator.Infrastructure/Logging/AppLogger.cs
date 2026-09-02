using System;
using System.Diagnostics;
using System.IO;

namespace KaraokeVideoCreator.Infrastructure.Logging
{
    public static class AppLogger
    {
        private static readonly object _lock = new object();
        private static readonly string _logFilePath;

        static AppLogger()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logDir = Path.Combine(appData, "KaraokeVideoCreator", "logs");
                Directory.CreateDirectory(logDir);
                _logFilePath = Path.Combine(logDir, "app.log");
            }
            catch
            {
                _logFilePath = "app.log";
            }
        }

        public static void LogInfo(string message) => WriteLog("INFO", message);
        public static void LogWarning(string message) => WriteLog("WARN", message);
        public static void LogError(string message, Exception? ex = null)
        {
            string fullMessage = ex != null ? $"{message} | Exception: {ex.Message}" : message;
            WriteLog("ERROR", fullMessage);
        }

        private static void WriteLog(string level, string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            Debug.WriteLine(line);

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine);
                }
                catch
                {
                    // Ignore logging failure to avoid crashing application
                }
            }
        }
    }
}
