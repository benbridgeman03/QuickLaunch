using QuickLaunch.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace QuickLaunch.Core.Services
{
    public class ScoringService
    {
        string[] helperKeywords =
        {
            "updater", "update",
            "report", "reporter",
            "error", "errorreporter",
            "helper", "service",
            "bootstrap", "install", "installer",
            "uninstall",
            "crash", "crashreporter", "crashhandler",
            "debug", "dbg", "info", "setup"
        };

        string[] userFolders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop).ToLower(),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToLower(),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToLower(),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos).ToLower(),
        };

        public int ScoreFile(IndexItem item, string rootPath)
        {
            int score = 0;
            score += GetExtensionScore(item);
            score += GetDepthScore(item, rootPath);
            score += GetNameScore(item.FullName);
            score += GetLastModifiedScore(item.LastModified);
            score += GetPathScore(item);

            return score;
        }

        private static int GetExtensionScore(IndexItem item)
        {
            return ScoringRules.ExtensionScores.TryGetValue(Path.GetExtension(item.Path).ToLower(), out int s) ? s : 0;
        }

        private static int GetDepthScore(IndexItem item, string rootPath)
        {
            var relative = Path.GetRelativePath(rootPath, item.Path);
            int depth = relative.Split(Path.DirectorySeparatorChar).Length - 1;
            return -depth * 5;
        }

        private int GetPathScore(IndexItem item)
        {
            string path = item.Path.ToLower();

            foreach (var folder in userFolders)
            {
                if (path.StartsWith(folder))
                    return 100;
            }
            return 0;
        }


        private int GetNameScore(string name)
        {
            if (name.Length > 50) return -50;
            if (name.All(c => char.IsLetterOrDigit(c) || c == '_')) return -10;
            if (helperKeywords.Any(k => name.ToLower().Contains(k)))
                return -100;
            return 0;
        }

        private static int GetLastModifiedScore(DateTime lastModified)
        {
            var ageDays = (DateTime.Now - lastModified).TotalDays;
            return ageDays < 30 ? 20 : 0;
        }
    }

}
