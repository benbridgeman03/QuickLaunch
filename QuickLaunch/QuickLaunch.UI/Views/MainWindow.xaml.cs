using QuickLaunch.Core;
using QuickLaunch.Core.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QuickLaunch.Core.Services;

namespace QuickLaunch.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private readonly FileIndexer _indexer;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const int WM_HOTKEY = 0x0312;


        public MainWindow(FileIndexer indexer)
        {
            InitializeComponent();

            _indexer = indexer;

            SearchTextBox.TextChanged += (s, e) =>
            {
                PlaceholderText.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                                             ? Visibility.Visible
                                             : Visibility.Collapsed;
            };

            SearchTextBox.GotFocus += (s, e) =>
            {
                PlaceholderText.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                                             ? Visibility.Visible
                                             : Visibility.Collapsed;
            };

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Width = SystemParameters.PrimaryScreenWidth * 0.5;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;

            Top -= 100;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim();

            PlaceholderText.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Hidden;

            if (string.IsNullOrEmpty(query))
            {
                SearchResults.ItemsSource = null;
                SearchResults.Visibility = Visibility.Collapsed;
                return;
            }

            int relevance = 0;

            var results = _indexer.Items
                .Where(i => !string.IsNullOrEmpty(i.FileName) &&
                           (i.FileName.Contains(query, StringComparison.OrdinalIgnoreCase)
                            || SearchService.GetFuzzyScore(query, i.FileName) > 50))
                .Select(i =>
                {
                    relevance = i.FileName != null ? SearchService.GetFuzzyScore(query, i.FileName) : 0;

                    if (!string.IsNullOrEmpty(i.FileName))
                    {
                        if (string.Equals(i.FileName, query, StringComparison.OrdinalIgnoreCase))
                            relevance += 50;
                        else if (i.FileName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                            relevance += 20;
                    }

                    return new
                    {
                        Item = i,
                        TotalScore = i.Score + relevance
                    };
                })
                .OrderByDescending(x => x.TotalScore)
                .Take(3)
                .Select(x => new SearchResultItem
                {
                    Display = $"{x.Item.FileName} – {x.Item.Type} - {relevance + x.TotalScore}",
                    Item = x.Item
                })
                .ToList();

            if (results.Any())
            {
                SearchResults.ItemsSource = results;
                SearchResults.Visibility = Visibility.Visible;
            }
            else
            {
                SearchResults.ItemsSource = null;
                SearchResults.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SearchResults.SelectedItem is SearchResultItem selected)
            {
                string filePath = selected.Item.Path;

                string? folder = System.IO.Path.GetDirectoryName(filePath);

                if (folder != null)
                {
                    try
                    {
                        Process.Start("explorer.exe", folder);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to open directory: {ex.Message}");
                    }
                }
            }
        }




        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(WndProc);

            RegisterHotKey(helper.Handle, 1, MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.Space));
            
            Hide();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == 1)
                {
                    ToggleLauncher();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void ToggleLauncher()
        {
            if (!IsVisible)
            {
                Show();
                Activate();
                Topmost = true;
                Topmost = false;
            }
            else
            {
                Hide();
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, 1);
            base.OnClosed(e);
        }
    }
}