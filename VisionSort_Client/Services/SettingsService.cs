using System.IO;
using System.Text.Json;
using VisionSort_Client.Models;

namespace VisionSort_Client.Services
{
    public class SettingsService
    {
        private readonly string _configPath = "appsettings.json";

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_configPath))
                return new AppSettings(); // 返回默认值

            string json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
    }
}