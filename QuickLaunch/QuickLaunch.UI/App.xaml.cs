using Microsoft.Extensions.DependencyInjection;
using QuickLaunch.Core;
using QuickLaunch.Core.Models;
using QuickLaunch.Core.Services;
using QuickLaunch.UI.Views;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace QuickLaunch.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            Services = serviceCollection.BuildServiceProvider();

            _ = BuildIndex();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private async Task BuildIndex()
        {
            var config = Services.GetRequiredService<ConfigService>();
            var indexer = Services.GetRequiredService<FileIndexer>();

            await Task.Run(async () =>
            {
                Debug.WriteLine("SEARCH PATH COUNT = " + config.Config.SearchPaths.Count);
                foreach (var path in config.Config.SearchPaths)
                {
                    Debug.WriteLine($"{path}");
                    if (Directory.Exists(path))
                        await indexer.BuildIndexAsync($"{path}");
                    else Debug.WriteLine($"Folder does not exist - {path}");
                }

                indexer.SaveToJson("index.json");
            });
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ScoringService>();
            services.AddSingleton<SearchService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<FileIndexer>();
            services.AddTransient<MainWindow>();
        }
    }
}
