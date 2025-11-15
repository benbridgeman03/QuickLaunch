using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickLaunch.Core.Models
{
    public class QuickLaunchConfig
    {
        public List<string> AllowedExtensions { get; set; } = new()
        {
            ".exe", ".lnk", ".bat", ".cmd", ".ps1",
            ".txt", ".pdf", ".docx", ".xlsx", ".pptx", ".url"
        };

        public List<string> SearchPaths { get; set; } = new()
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs",
            @"C:\Program Files (x86)",
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            @"C:\Windows\System32",
            @"C:\Program Files\WindowsApps\",
        };

        public HashSet<string> IgnoredFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "library",
            "packagecache",
            "temp",
            "obj",
            "build",
            "logs",
            "node_modules",
            ".git",
            ".idea",
            ".vscode",
            "bin",
            "obj",
            "Recent"
        };
    }
}
