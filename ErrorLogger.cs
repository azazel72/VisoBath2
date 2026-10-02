using System;
using System.Collections.Generic;
using System.IO;

namespace VisoBath
{
    internal static class ErrorLogger
    {
        private static readonly object syncRoot = new object();
        private static readonly List<string> entries = new List<string>();
        private static string currentLogDate = string.Empty;
        private static string currentLogPath = string.Empty;

        public static event Action<string> EntryAdded;

        public static IReadOnlyList<string> Entries
        {
            get
            {
                lock (syncRoot)
                {
                    return entries.AsReadOnly();
                }
            }
        }

        public static void Add(string message)
        {
            Add(message, null);
        }

        public static void Add(string message, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            string fileEntry = entry;
            if (exception != null)
            {
                fileEntry += Environment.NewLine + exception.ToString();
            }

            lock (syncRoot)
            {
                entries.Add(entry);
                if (entries.Count > 1000)
                {
                    entries.RemoveRange(0, entries.Count - 1000);
                }

                WriteToFile(fileEntry);
            }

            EntryAdded?.Invoke(entry);
        }

        private static void WriteToFile(string text)
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            if (currentLogDate != today || string.IsNullOrWhiteSpace(currentLogPath))
            {
                currentLogDate = today;
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                currentLogPath = Path.Combine(logDir, $"VisoBath_{today}.log");
            }

            File.AppendAllText(currentLogPath, text + Environment.NewLine + Environment.NewLine);
        }
    }
}
