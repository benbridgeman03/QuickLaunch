using Microsoft.Win32;
using QuickLaunch.Core.Models;
using QuickLaunch.Core.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Shell32;

namespace QuickLaunch.Core
{
    public class FileIndexer
    {
        private readonly ConcurrentDictionary<string, IndexItem> _itemsByPath = new();
        private Dictionary<string, IndexItem> _previousIndex = new();
        public IEnumerable<IndexItem> Items => _itemsByPath.Values;

        private readonly HashSet<string> _allowed;
        private readonly HashSet<string> _ignoredFolders;
        private readonly ConfigService _config;
        private readonly ScoringService _score;
        private string _rootFolder = "";

        public FileIndexer(ConfigService config, ScoringService score)
        {
            _config = config;
            _score = score;
            _allowed = config.Config.AllowedExtensions.Select(e => e.ToLower()).ToHashSet();
            _ignoredFolders = config.Config.IgnoredFolders.Select(e => e.ToLower()).ToHashSet();
        }

        public void Initialize(string jsonPath)
        {
            LoadExistingIndex(jsonPath);
        }

        public async Task IndexUwpAppsAsync()
        {
            await Task.Run(() => IndexUwpApps());
        }

        public async Task IndexDirectoryAsync(string rootPath)
        {
            if (!Directory.Exists(rootPath)) return;

            _rootFolder = Path.GetFullPath(rootPath);
            var queue = new ConcurrentQueue<string>();
            queue.Enqueue(rootPath);

            int workerCount = Environment.ProcessorCount * 2;
            var workers = Enumerable.Range(0, workerCount)
                                    .Select(_ => Task.Run(() => Worker(queue)))
                                    .ToList();

            await Task.WhenAll(workers);
        }

        public void LoadExistingIndex(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                _previousIndex = new();
                return;
            }

            try
            {
                var json = File.ReadAllText(jsonPath);
                var items = JsonSerializer.Deserialize<List<IndexItem>>(json);

                if (items != null)
                {
                    _previousIndex = items.ToDictionary(i => i.Path, i => i);
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.FileName))
                        {
                            string key = item.FileName.ToLowerInvariant();
                            _itemsByPath[key] = item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading index: {ex.Message}");
                _previousIndex = new();
            }
        }

        public void RemovePath(string rootPath)
        {
            var itemsToRemove = _itemsByPath
                .Where(kvp => kvp.Value.Path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in itemsToRemove)
            {
                _itemsByPath.TryRemove(key, out _);
            }
        }

        public void SaveToJson(string filePath)
        {
            Debug.WriteLine("Saving JSON");
            var sorted = _itemsByPath.Values.OrderByDescending(i => i.Score).ToList();
            var json = JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
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

        private bool ProcessDirectory(string path, ConcurrentQueue<string> queue)
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    var info = new DirectoryInfo(dir);
                    if (IsHiddenOrSystem(info) || _ignoredFolders.Contains(info.Name.ToLower()))
                        continue;

                    var dirItem = new IndexItem
                    {
                        FileName = info.Name,
                        FullName = info.Name,
                        Path = dir,
                        Type = ItemType.Directory,
                        LastModified = info.LastWriteTime,
                        LastAccessed = info.LastAccessTime,
                    };

                    dirItem.Score = _score.ScoreFile(dirItem, _rootFolder);
                    _itemsByPath[dir] = dirItem;

                    queue.Enqueue(dir);
                }

                foreach (var file in Directory.EnumerateFiles(path))
                {
                    var info = new FileInfo(file);
                    string ext = info.Extension.ToLower();

                    if (!_allowed.Contains(ext) || IsFilteredSystem32(file))
                        continue;

                    _previousIndex.TryGetValue(info.FullName, out var oldEntry);

                    if (oldEntry != null && oldEntry.LastModified == info.LastWriteTime)
                    {
                        string keyOld = oldEntry.FileName.ToLowerInvariant();
                        _itemsByPath.AddOrUpdate(keyOld, oldEntry, (_, existing) => oldEntry);
                        continue;
                    }

                    string description = oldEntry?.Desc ?? "";
                    if (string.IsNullOrEmpty(description) && ext == ".exe")
                    {
                        try { description = FileVersionInfo.GetVersionInfo(file).FileDescription ?? ""; }
                        catch { }
                    }

                    var item = new IndexItem
                    {
                        FileName = Path.GetFileNameWithoutExtension(info.Name),
                        FullName = info.Name,
                        Path = info.FullName,
                        Desc = description,
                        Type = GetItemType(ext),
                        LastModified = info.LastWriteTime,
                        LastAccessed = info.LastAccessTime
                    };

                    item.Score = _score.ScoreFile(item, _rootFolder);

                    var key = item.FileName.ToLowerInvariant();
                    _itemsByPath.AddOrUpdate(key, item, (_, existing) => item.Score > existing.Score ? item : existing);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void IndexUwpApps()
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic appsFolder = shell.NameSpace("shell:Appsfolder");

                foreach (dynamic item in appsFolder.Items())
                {
                    string appName = item.Name;
                    if (string.IsNullOrWhiteSpace(appName)) continue;

                    string appId = item.ExtendedProperty("System.AppUserModel.ID") as string;
                    if (string.IsNullOrEmpty(appId)) continue;

                    var uwpItem = new IndexItem
                    {
                        FileName = appName,
                        FullName = appName,
                        Path = appId,
                        Desc = "",
                        Type = ItemType.UWP,
                        LastModified = DateTime.Now,
                        LastAccessed = DateTime.Now,
                        Score = 150
                    };

                    string key = uwpItem.FileName.ToLowerInvariant();
                    _itemsByPath.AddOrUpdate(key, uwpItem, (_, existing) => uwpItem.Score > existing.Score ? uwpItem : existing);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to index UWP apps: {ex.Message}");
            }
        }

        private static bool IsHiddenOrSystem(FileSystemInfo info)
        {
            return (info.Attributes & FileAttributes.Hidden) != 0;
        }

        private static ItemType GetItemType(string ext) =>
            ext switch
            {
                ".exe" => ItemType.Exe,
                ".lnk" => ItemType.Shortcut,
                ".url" => ItemType.Shortcut,
                _ => ItemType.File
            };

        private static bool IsFilteredSystem32(string path)
        {
            if (!path.StartsWith(@"C:\Windows\System32", StringComparison.OrdinalIgnoreCase))
                return false;

            string name = Path.GetFileName(path);
            HashSet<string> allowed = new(StringComparer.OrdinalIgnoreCase)
            {
                "notepad.exe","calc.exe","mspaint.exe","cmd.exe","powershell.exe",
                "taskmgr.exe","explorer.exe","snippingtool.exe"
            };
            return !allowed.Contains(name);
        }
    }
}