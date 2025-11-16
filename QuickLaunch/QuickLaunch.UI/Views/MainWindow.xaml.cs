using QuickLaunch.Core;
using QuickLaunch.Core.Models;
using QuickLaunch.Core.Services;
using System.Diagnostics;
using System.IO;
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
using Path = System.IO.Path;

namespace QuickLaunch.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private readonly FileIndexer _indexer;
        private readonly SearchService _search;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const int WM_HOTKEY = 0x0312;


        public MainWindow(FileIndexer indexer, SearchService search)
        {
            InitializeComponent();

            _indexer = indexer;
            _search = search;

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

            ContentRendered += MainWindow_ContentRendered;
        }

        private void MainWindow_ContentRendered(object? sender, EventArgs e)
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

            var results = _search.SearchItem(query);

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
                string path = selected.Item.Path;

                if (string.IsNullOrEmpty(path))
                    return;

                try
                {
                    if (Directory.Exists(path))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                    }
                    else if (File.Exists(path))
                    {
                        string ext = Path.GetExtension(path).ToLower();

                        if (ext == ".exe")
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = path,
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = path,
                                UseShellExecute = true
                            });
                        }
                    }
                    else
                    {
                        string appUserModelId = selected.Item.Path;
                        if (!string.IsNullOrEmpty(appUserModelId))
                        {
                            var psi = new ProcessStartInfo("explorer.exe", $"shell:appsFolder\\{appUserModelId}")
                            {
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                        }
                    }

                    ToggleLauncher();
                }

                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to open item: {ex.Message}");
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