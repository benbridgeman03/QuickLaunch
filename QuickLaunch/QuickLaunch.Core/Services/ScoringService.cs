using QuickLaunch.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace QuickLaunch.Core.Services
{
    public class ScoringService
    {

        string[] userFolders =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop).ToLower(),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToLower(),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToLower(),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos).ToLower(),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ];

        public int ScoreFile(IndexItem item, string rootPath)
        {
            int score = 0;
            score += GetExtensionScore(item);
            score += GetDepthScore(item, rootPath);
            score += GetNameScore(item.FullName);
            score += GetLastModifiedScore(item.LastModified);
            score += GetPathScore(item);
            score += GetLastUsed(item);

            return score;
        }

        private static int GetExtensionScore(IndexItem item)
        {
            try
            {
                if (item.Type == ItemType.Directory)
                    return 40;
                if (item.Type == ItemType.UWP)
                    return 100;
                return ScoringRules.ExtensionScores.TryGetValue(Path.GetExtension(item.Path).ToLower(), out int s) ? s : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static int GetDepthScore(IndexItem item, string rootPath)
        {
            try
            {
                var relative = Path.GetRelativePath(rootPath, item.Path);
                int depth = relative.Split(Path.DirectorySeparatorChar).Length - 1;
                return -depth * 20;
            }
            catch { return 0; }
        }

        private int GetPathScore(IndexItem item)
        {
            try
            {
                string path = item.Path.ToLowerInvariant();

                foreach (var folder in userFolders)
                {
                    string folderPath = folder.TrimEnd('\\').ToLowerInvariant();
                    if (path.StartsWith(folderPath + "\\") || path == folderPath)
                        return 50;
                }
            }
            catch { return 0; }

            return 0;
        }

        private int GetNameScore(string name)
        {
            try
            {
                if (name.Length > 50) return -50;
                if (name.All(c => char.IsLetterOrDigit(c) || c == '_')) return -10;
                if (ScoringRules.helperKeywords.Any(k => name.ToLower().Contains(k)))
                    return -100;
            }
            catch { return 0; }

            return 0;
        }

        private static int GetLastModifiedScore(DateTime lastModified)
        {
            try
            {
                var ageDays = (DateTime.Now - lastModified).TotalDays;
                return ageDays < 30 ? 20 : 0;
            }
            catch { return 0; }
        }

        private static int GetLastUsed(IndexItem item)
        {
            try
            {
                var ageDays = (DateTime.Now - item.LastAccessed).TotalDays;
                return ageDays < 30 ? 20 : 0;
            }
            catch { return 0; }
        }
    }

}
