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
    public partial class App : System.Windows.Application
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
                Debug.WriteLine("--- Starting Indexing ---");

                indexer.Initialize("index.json");

                await indexer.IndexUwpAppsAsync();

                Debug.WriteLine("SEARCH PATH COUNT = " + config.Config.SearchPaths.Count);
                foreach (var path in config.Config.SearchPaths)
                {
                    Debug.WriteLine($"Indexing: {path}");
                    await indexer.IndexDirectoryAsync(path);
                }

                indexer.SaveToJson("index.json");

                Debug.WriteLine("--- Indexing Complete ---");
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
