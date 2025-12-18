using QuickLaunch.Core; // For FileIndexer
using QuickLaunch.Core.Models;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;

namespace QuickLaunch.UI.Views
{
    public partial class SettingsWindow : Window
    {
        private QuickLaunchConfig _config;
        private FileIndexer _indexer; // Store the indexer

        // Update Constructor to accept FileIndexer
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
            PathsListBox.ItemsSource = _config.SearchPaths;
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

        private void SaveClose_Click(object sender, RoutedEventArgs e)
        {
            _config.Save();
            _indexer.SaveToJson("index.json");
            this.Close();
        }
    }
}