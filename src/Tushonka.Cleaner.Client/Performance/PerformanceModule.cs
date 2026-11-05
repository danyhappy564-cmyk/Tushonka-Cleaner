#nullable enable
using System;
using System.IO;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Tushonka.Cleaner.Client.Performance.Compat;
using Tushonka.Cleaner.Client.Performance.Telemetry;
#if BENCH
using System.Reflection;
using HarmonyLib;
#endif

namespace Tushonka.Cleaner.Client.Performance;

internal sealed class PerformanceModule
{
    public const string ModGuid    = CleanerPlugin.ModGuid;
    public const string ModName    = CleanerPlugin.ModName;
    public const string ModVersion = CleanerPlugin.ModVersion;

    public static CleanerConfig LoadedConfig { get; private set; } = new();
    public static DetectedMods Mods { get; private set; } = new();
    public static ManualLogSource? Log { get; private set; }
    public static string? SptUserLogsDir { get; private set; }

    internal static PerformanceModule? Instance { get; private set; }
    private ServerSettingsMenu? _menu;
    internal IReadOnlyList<ConfigEntryBase> Settings { get; private set; } = Array.Empty<ConfigEntryBase>();
    internal bool Ready => _menu != null;
    internal bool Connected => _menu?.Connected ?? false;
    internal string Status => _menu?.StatusText ?? "";
    internal string LastResult => _menu?.LastResult ?? "";
    internal void Refresh() => _menu?.RequestRefresh();
    internal void SetLanguage(string language) => _menu?.SetLanguage(language);

    internal void Initialize(ConfigFile config, ManualLogSource logger)
    {
        Instance = this;
        Log = logger;
        Log.LogInfo($"{ModName} v{ModVersion} loading");

        try
        {
            var (loaded, sptLogsDir) = ConfigLoader.Load();
            LoadedConfig = loaded;
            SptUserLogsDir = sptLogsDir;
            ClientTelemetry.TimingEnabled = LoadedConfig.Telemetry.TimingEnabled;

            Mods = ModDetector.Scan();
            if (LoadedConfig.Compat.Verbose)
            {
                Log.LogInfo($"{ModName} compat scan: SAIN={Mods.HasSain} AILimit={Mods.HasAiLimit} PerfImprovements={Mods.HasPerformanceImprovements} Tyfon={Mods.HasTyfonUIFixes}");
            }

#if BENCH
            // C6: frame-stats benchmark recorder, the only client feature since the
            // in-raid log suppressor was removed in 1.3 (its value never survived
            // measurement, and suppressing other mods' in-raid logs costs the
            // ecosystem more than it saves). Compiled only into dev builds
            // (-p:Bench=true); release packages ship NO client plugin at all.
            // Sampling rides EFT's own GameWorldUnityTickListener via Harmony
            // postfix — see FrameStatsRecorder doc for why we don't use our own
            // MonoBehaviour. masterEnabled is still read so each stats line tags
            // which side of an A/B run it belongs to.
            FrameStatsRecorder.Enabled = LoadedConfig.Client.FrameStats.Enabled;
            if (FrameStatsRecorder.Enabled)
            {
                Log.LogInfo($"{ModName} [C6/BENCH] frame-stats recorder armed — per-raid FPS stats will append to user/logs/CompoundingPerf-framestats.jsonl");
            }

            var harmony = new Harmony(ModGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
#endif

            // F12 page for the server mod's settings, applied live through the server's
            // settings route. The only release-build feature of this plugin.
            var previous = new HashSet<ConfigDefinition>(config.Keys);
            _menu = new ServerSettingsMenu(config, logger);
            Settings = config.Keys.Where(k => !previous.Contains(k)).Select(k => config[k]).ToArray();
            ClientFixes.Apply(logger);

            Log.LogInfo($"{ModName} ready");
        }
        catch (Exception ex)
        {
            Log.LogError($"{ModName} failed to initialize: {ex}");
        }
    }

    internal void Tick()
    {
        if (_menu == null) return;
        _menu?.Update();
        ClientFixes.Tick();
    }

    internal void Shutdown()
    {
        if (!LoadedConfig.Telemetry.Enabled || string.IsNullOrEmpty(SptUserLogsDir)) return;
        try { TelemetryDumper.Dump(SptUserLogsDir!, "shutdown"); }
        catch { /* best-effort */ }
    }
}
