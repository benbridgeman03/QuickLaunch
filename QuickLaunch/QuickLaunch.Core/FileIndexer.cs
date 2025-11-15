using QuickLaunch.Core.Models;
using QuickLaunch.Core.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace QuickLaunch.Core
{
    public class FileIndexer
    {
        private readonly ConcurrentDictionary<string, IndexItem> _itemsByName = new();

        public IEnumerable<IndexItem> Items => _itemsByName.Values;

        private readonly HashSet<string> _allowed;
        private readonly HashSet<string> _ignoredFolders;

        private readonly ConfigService _config;
        private readonly ScoringService _score;

        private string _rootFolder = "";

        public FileIndexer(ConfigService config, ScoringService score)
        {
            _config = config;
            _score = score;

            _allowed = config.Config.AllowedExtensions
                .Select(e => e.ToLower())
                .ToHashSet();

            _ignoredFolders = config.Config.IgnoredFolders
                .Select(e => e.ToLower())
                .ToHashSet();
        }

        public async Task BuildIndexAsync(string rootPath)
        {
            _rootFolder = Path.GetFullPath(rootPath);

            var folderQueue = new ConcurrentQueue<string>();
            folderQueue.Enqueue(rootPath);

            int maxWorkers = Environment.ProcessorCount * 2;
            var tasks = new List<Task>();

            for (int i = 0; i < maxWorkers; i++)
            {
                tasks.Add(Task.Run(() => Worker(folderQueue)));
            }

            await Task.WhenAll(tasks);
        }

        private void Worker(ConcurrentQueue<string> queue)
        {
            while (queue.TryDequeue(out string? current))
            {
                try
                {
                    ProcessDirectory(current, queue);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR in {current}: {ex.Message}");
                }
            }
        }

        private void ProcessDirectory(string path, ConcurrentQueue<string> queue)
        {
            IEnumerable<string> dirs;
            try
            {
                dirs = Directory.EnumerateDirectories(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Cannot access directory: {path} -> {ex.Message}");
                return;
            }

            foreach (var dir in dirs)
            {
                var info = new DirectoryInfo(dir);

                // Skip hidden/system DIRECTORIES only
                if (IsHiddenOrSystem(info))
                    continue;

                if (_ignoredFolders.Contains(info.Name.ToLower()))
                    continue;

                queue.Enqueue(dir);
            }

            bool folderHasApprovedFiles = false;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Cannot access directory: {path} -> {ex.Message}");
                return;
            }

            foreach (var file in files)
            {
                var info = new FileInfo(file);

                string ext = info.Extension.ToLower();
                if (!_allowed.Contains(ext))
                    continue;

                folderHasApprovedFiles = true;

                ItemType type = ext switch
                {
                    ".exe" => ItemType.Exe,
                    ".lnk" => ItemType.Shortcut,
                    ".url" => ItemType.Shortcut,
                    _ => ItemType.File
                };

                var item = new IndexItem
                {
                    FileName = Path.GetFileNameWithoutExtension(info.Name),
                    FullName = info.Name,
                    Path = info.FullName,
                    Type = type,
                    LastModified = info.LastWriteTime,
                };

                item.Score = _score.ScoreFile(item, _rootFolder);

                var key = item.FileName.ToLowerInvariant();

                _itemsByName.AddOrUpdate(
                    key,
                    item,
                    (k, existing) => item.Score > existing.Score ? item : existing
                );
            }

            var dirInfo = new DirectoryInfo(path);

            if (folderHasApprovedFiles)
            {
                var dirItem = new IndexItem
                {
                    FileName = Path.GetFileName(path),
                    FullName = Path.GetFileName(path),
                    Path = path,
                    Type = ItemType.Directory,
                    LastModified = dirInfo.LastWriteTime
                };

                var dirKey = $"{dirItem.FileName}|{dirItem.Type}";
                _itemsByName.TryAdd(dirKey, dirItem);
            }

        }


        private static bool IsHiddenOrSystem(FileSystemInfo info)
        {
            var attr = info.Attributes;
            return (attr & FileAttributes.Hidden) != 0;
        }

        public void SaveToJson(string filePath)
        {
            Debug.Write("Finished Indexing, Creating JSON");

            var sortedItems = Items.OrderByDescending(i => i.Score).ToList();

            string json = JsonSerializer.Serialize(sortedItems, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
    }
}
