using System.Text.Json;
using Serilog;

namespace TimeTracker.Core.Configuration;

/// <summary>
/// Загрузка/сохранение настроек трекера в JSON-файле рядом с приложением.
/// </summary>
public sealed class SettingsStore
{
    private readonly string _filePath;
    private readonly ILogger _log;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SettingsStore(string filePath, ILogger log)
    {
        _filePath = filePath;
        _log = log;
    }

    public TrackerSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new TrackerSettings();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<TrackerSettings>(json, JsonOptions);
            return settings ?? new TrackerSettings();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось загрузить настройки — используются значения по умолчанию");
            return new TrackerSettings();
        }
    }

    public void Save(TrackerSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_filePath, json);
            _log.Information("Настройки сохранены");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось сохранить настройки");
        }
    }
}
