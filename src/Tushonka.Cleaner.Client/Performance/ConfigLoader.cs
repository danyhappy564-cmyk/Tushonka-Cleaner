#nullable enable
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Tushonka.Cleaner.Client.Performance;

/// <summary>
/// Locates and parses the same <c>config.json</c> the server mod uses. Tries the canonical
/// SPT mods path first, falls back to a sidecar <c>config.json</c> next to the plugin DLL.
/// Returns the parsed config and the SPT user-logs directory (used by telemetry dumper).
/// </summary>
internal static class ConfigLoader
{
    public static (CleanerConfig config, string? sptUserLogsDir) Load()
    {
        var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        // BepInEx\plugins\<this>\..\..\..  → SPT root
        var sptRoot = Path.GetFullPath(Path.Combine(pluginDir, "..", "..", ".."));
        var serverModConfig = Path.Combine(sptRoot, "SPT_Runtime", "user", "mods", "Tushonka-Cleaner", "config.json");
        var sidecarConfig   = Path.Combine(pluginDir, "config.json");
        var logsDir         = Path.Combine(sptRoot, "SPT_Runtime", "user", "logs");

        var path = File.Exists(serverModConfig) ? serverModConfig
                 : File.Exists(sidecarConfig)   ? sidecarConfig
                 : null;

        if (path is null)
        {
            PerformanceModule.Log?.LogWarning("Tushonka.Cleaner.Client.Performance could not locate config.json — falling back to defaults");
            return (new CleanerConfig(), Directory.Exists(logsDir) ? logsDir : null);
        }

        PerformanceModule.Log?.LogInfo($"Tushonka.Cleaner.Client.Performance reading config from {path}");
        var raw = File.ReadAllText(path);
        var parsed = JsonConvert.DeserializeObject<CleanerConfig>(raw) ?? new CleanerConfig();
        return (parsed, Directory.Exists(logsDir) ? logsDir : null);
    }
}
