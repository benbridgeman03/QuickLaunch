using QuickLaunch.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace QuickLaunch.Core.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        public QuickLaunchConfig Config { get; private set; }

        public ConfigService()
        {
            Config = QuickLaunchConfig.Load();
        }

        public void Load()
        {
            Config = new QuickLaunchConfig();
            Save();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_configPath, json);
        }
    }
}
