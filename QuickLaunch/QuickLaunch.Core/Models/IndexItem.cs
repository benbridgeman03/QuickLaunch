using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickLaunch.Core.Models
{
    public enum ItemType 
    { 
        File,
        Exe,
        Directory,
        Shortcut,
    }

    public class IndexItem
    {
        public string? FileName { get; set; }
        public required string FullName { get; set; }
        public required string Path { get; set; }
        public required ItemType Type { get; set; }
        public DateTime LastModified { get; set; }
        public int Score { get; set; }
    }
}
