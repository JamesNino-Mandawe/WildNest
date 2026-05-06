using System;
using System.Diagnostics;
using System.IO;

namespace Project
{
    internal static class ProjectDiagnostics
    {
        internal static void LogInfo(string source, string message)
        {
            Write("INFO", source, message);
        }

        internal static void LogWarning(string source, string message)
        {
            Write("WARN", source, message);
        }

        internal static void LogError(string source, Exception ex, string? context = null)
        {
            string suffix = string.IsNullOrWhiteSpace(context) ? string.Empty : $" | {context}";
            Write("ERROR", source, $"{ex.Message}{suffix}");
        }

        private static void Write(string level, string source, string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{source}] {message}";
            Debug.WriteLine(line);

            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WildNest",
                    "logs");
                Directory.CreateDirectory(dir);
                string logPath = Path.Combine(dir, $"wildnest-{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch
            {
                // Diagnostics must never interrupt the user flow.
            }
        }
    }
}
