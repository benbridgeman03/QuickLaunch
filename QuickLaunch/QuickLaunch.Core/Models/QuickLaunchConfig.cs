using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuickLaunch.Core.Models
{
    public class QuickLaunchConfig
    {
        public HashSet<string> HiddenFiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> HiddenAppNames { get; set; } = new(StringComparer.OrdinalIgnoreCase); 


        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickLaunch",
            "config.json");

        public List<string> AllowedExtensions { get; set; } = new()
        {
            ".exe", ".lnk", ".bat", ".cmd", ".ps1",
            ".txt", ".pdf", ".docx", ".xlsx", ".pptx", ".url", ".xls"
        };

        public List<string> SearchPaths { get; set; } = new()
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
        };

        public HashSet<string> IgnoredFolders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", ".git", "bin", "obj", "temp"
        };


        public static QuickLaunchConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaults = new QuickLaunchConfig();
                defaults.Save();
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<QuickLaunchConfig>(json) ?? new QuickLaunchConfig();
            }
            catch
            {
                return new QuickLaunchConfig();
            }
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}