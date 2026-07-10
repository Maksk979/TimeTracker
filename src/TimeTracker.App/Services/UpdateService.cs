using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Serilog;

namespace TimeTracker.App.Services;

/// <summary>
/// Информация о релизе GitHub.
/// </summary>
public sealed class GitHubRelease
{
    public string TagName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public List<GitHubAsset> Assets { get; set; } = new();
}

/// <summary>
/// Ассет релиза (файл для скачивания).
/// </summary>
public sealed class GitHubAsset
{
    public string Name { get; set; } = string.Empty;
    public string BrowserDownloadUrl { get; set; } = string.Empty;
    public long Size { get; set; }
}

/// <summary>
/// Сервис автообновления через GitHub Releases.
/// Проверяет наличие новых релизов и скачивает exe.
/// </summary>
public sealed class UpdateService : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger _log;
    private readonly string _repoOwner;
    private readonly string _repoName;

    /// <summary>Текущая версия приложения.</summary>
    public Version CurrentVersion { get; }

    public UpdateService(ILogger log, string repoOwner = "Maksk979", string repoName = "TimeTracker")
    {
        _log = log;
        _repoOwner = repoOwner;
        _repoName = repoName;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("TimeTracker-Updater");

        // Версия берётся из сборки
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        CurrentVersion = version ?? new Version(1, 0, 0);
    }

    /// <summary>
    /// Проверяет наличие обновления. Возвращает null если обновлений нет.
    /// </summary>
    public async Task<GitHubRelease?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (release == null) return null;

            // Сравниваем версии
            var latestVersion = ParseVersion(release.TagName);
            if (latestVersion != null && latestVersion > CurrentVersion)
            {
                _log.Information("Доступно обновление: {Version}", release.TagName);
                return release;
            }

            _log.Information("Обновлений нет. Текущая версия: {Version}", CurrentVersion);
            return null;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Не удалось проверить обновления");
            return null;
        }
    }

    /// <summary>
    /// Скачивает exe файл обновления. Возвращает путь к скачанному файлу.
    /// </summary>
    public async Task<string> DownloadUpdateAsync(GitHubRelease release, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (asset == null)
            throw new InvalidOperationException("В релизе нет exe файла");

        var tempPath = Path.Combine(Path.GetTempPath(), $"TimeTracker_update_{release.TagName}.exe");

        _log.Information("Скачивание обновления: {Url}", asset.BrowserDownloadUrl);

        using var response = await _http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
        var totalBytesRead = 0L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);

        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalBytesRead += bytesRead;

            if (totalBytes > 0)
            {
                var percent = (int)(totalBytesRead * 100 / totalBytes);
                progress?.Report(percent);
            }
        }

        _log.Information("Обновление скачано: {Path}", tempPath);
        return tempPath;
    }

    /// <summary>
    /// Устанавливает обновление: закрывает текущий процесс и запускает новый exe.
    /// </summary>
    public void ApplyUpdate(string updateExePath)
    {
        _log.Information("Применение обновления: {Path}", updateExePath);

        // Запускаем новый exe с параметром --update
        var currentExe = Environment.ProcessPath ?? "";
        var startInfo = new ProcessStartInfo
        {
            FileName = updateExePath,
            Arguments = $"--update \"{currentExe}\"",
            UseShellExecute = true,
        };

        Process.Start(startInfo);
    }

    private static Version? ParseVersion(string tagName)
    {
        // Убираем префикс 'v' если есть
        var versionStr = tagName.TrimStart('v', 'V');
        return Version.TryParse(versionStr, out var version) ? version : null;
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
