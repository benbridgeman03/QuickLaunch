using QuickLaunch.Core; // For FileIndexer
using QuickLaunch.Core.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;

namespace QuickLaunch.UI.Views
{
    public partial class SettingsWindow : Window
    {
        private QuickLaunchConfig _config;
        private FileIndexer _indexer;
        private bool _requiresRescan = false;

        public SettingsWindow(QuickLaunchConfig config, FileIndexer indexer)
        {
            InitializeComponent();
            _config = config;
            _indexer = indexer;
            RefreshList();
        }

        private void RefreshList()
        {
            PathsListBox.ItemsSource = null;
            PathsListBox.ItemsSource = _config.SearchPaths.ToList();

            var allHidden = new List<string>();

            if (_config.HiddenAppNames != null)
                allHidden.AddRange(_config.HiddenAppNames);

            if (_config.HiddenFiles != null)
                allHidden.AddRange(_config.HiddenFiles);

            HiddenFilesListBox.ItemsSource = null;
            HiddenFilesListBox.ItemsSource = allHidden;
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                WinForms.DialogResult result = dialog.ShowDialog();
                if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    string newPath = dialog.SelectedPath;

                    if (!_config.SearchPaths.Contains(newPath))
                    {
                        _config.SearchPaths.Add(newPath);
                        RefreshList();

                        await _indexer.IndexDirectoryAsync(newPath);
                    }
                }
            }
        }

        private void RemovePath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string pathToRemove)
            {
                _config.SearchPaths.Remove(pathToRemove);
                RefreshList();

                _indexer.RemovePath(pathToRemove);
            }
        }

        private void UnhideFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string itemToUnhide)
            {
                bool changed = false;


                if (_config.HiddenAppNames != null && _config.HiddenAppNames.Contains(itemToUnhide))
                {
                    _config.HiddenAppNames.Remove(itemToUnhide);
                    changed = true;
                }

                if (_config.HiddenFiles != null && _config.HiddenFiles.Contains(itemToUnhide))
                {
                    _config.HiddenFiles.Remove(itemToUnhide);
                    changed = true;
                }

                if (changed)
                {
                    RefreshList();
                    _requiresRescan = true;
                }
            }
        }

        private void SaveClose_Click(object sender, RoutedEventArgs e)
        {
  
            if (sender is System.Windows.Controls.Button btn) btn.IsEnabled = false;

            _config.Save();
            _indexer.SaveToJson("index.json");

            if (_requiresRescan)
            {
                Task.Run(() => _indexer.FullScanAsync());
            }

            this.Close();
        }
    }
}