using System.IO;
using System.Text.Json;

namespace AthleticsCertificateMaker.GenerateCertificateApp.Services;

public sealed class AppRuntimeSettings
{
    public string SettingsJsonPath { get; set; } = string.Empty;
    public string ResultsPath { get; set; } = string.Empty;
    public string TemplatePath { get; set; } = string.Empty;
}

public sealed class AppRuntimeSettingsService
{
    private readonly string _settingsFilePath;

    public AppRuntimeSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "AthleticsCertificateMaker", "GenerateCertificateApp");
        Directory.CreateDirectory(folder);
        _settingsFilePath = Path.Combine(folder, "appsettings.json");
    }

    public AppRuntimeSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppRuntimeSettings();
            }

            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppRuntimeSettings>(json) ?? new AppRuntimeSettings();
        }
        catch
        {
            return new AppRuntimeSettings();
        }
    }

    public void Save(AppRuntimeSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsFilePath, json);
    }
}
