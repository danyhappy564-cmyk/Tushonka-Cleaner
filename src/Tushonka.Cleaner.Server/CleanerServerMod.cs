using System.Reflection;
using Tushonka.Cleaner.Server.Diagnostics;
using Tushonka.Cleaner.Server.Features;
using Tushonka.Cleaner.Server.Telemetry;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Server;
using SPTarkov.Server.Core.Utils.Cloners;

namespace Tushonka.Cleaner.Server;

/// <summary>SPT mod metadata. No package.json — this record replaces it. 4.1 swapped the
/// abstract <c>AbstractModMetadata</c> base for the <c>IModMetadata</c> interface and
/// added <c>HasPrepatcher</c>.</summary>
public record ModMetadata : IModMetadata, SPTarkov.Server.Web.IModBlazorMetadata
{
    public string ModGuid { get; init; } = CleanerServerMod.ModGuid;
    public string Name { get; init; } = "Tushonka Cleaner";
    public string Author { get; init; } = "R_F";
    public SemanticVersioning.Version Version { get; init; } = new("1.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.5");
    public string License { get; init; } = "GPL-3.0";
    public bool HasPrepatcher { get; init; } = false;

    public List<string>? Contributors { get; init; } = ["CactusPie", "matsix", "EchoStarz"];
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }

    // The launcher's / SPT web panel's "mod pages" list links here (Web/WebPageController).
    public string? WWWRootUrl { get; init; }
    public string? HomePage { get; init; } = "/tushonka-cleaner/";
    public string? HomePageDescription { get; init; } = "메모리·서버 최적화 설정 · Memory and server settings";
}

/// <summary>
/// Server-side entry for CompoundingPerf. Reads <c>config.json</c> and installs the
/// individual features.
///
/// <para><b>Why everything is Harmony now.</b> Through 4.0 every feature here was a DI
/// subclass registered with <c>Injectable.TypeOverride</c> — normal virtual dispatch, no
/// IL surgery, and other mods' Harmony patches on those classes kept working because our
/// subclass <i>was</i> the object they patched. SPT 4.1 removed that option: the
/// <c>TypeOverride</c> property is gone from the attribute, <c>SaveServer</c>,
/// <c>RagfairServer</c>, <c>RandomUtil</c> and <c>SptWebSocketConnectionHandler</c> are
/// sealed, and not one of the methods this mod used to override is virtual any more.
/// Deriving and overriding is simply not a thing that can be done on 4.1.</para>
///
/// <para>So the surviving features are Harmony patches, kept as small as the job allows —
/// two of them rewrite a single call or constant rather than replace a method body, which
/// makes them behaviour-neutral by construction instead of by careful re-implementation.
/// Six features were retired rather than translated, because 4.1 does the job itself; see
/// <c>ServerToggles</c> for the per-feature evidence.</para>
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 50)]
public class CleanerServerMod(
    ISptLogger<CleanerServerMod> logger,
    ModHelper modHelper,
    ICloner cloner) : IOnLoad
{
    public const string ModGuid = "com.zzap.tushonka.cleaner";

    // zip에는 기본값(config.default.json)만 있습니다. 덮어쓰기 설치가 config.json을 건드리지 않아 서버 설정이 유지됩니다.
    private static void EnsureConfigFile(string modPath)
    {
        var target = Path.Combine(modPath, "config.json");
        var defaults = Path.Combine(modPath, "config.default.json");
        if (!File.Exists(target) && File.Exists(defaults)) File.Copy(defaults, target);
    }

    public Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var conflicts = LegacyServerGuard.Scan(modPath, message => logger.Warning(message));
            EnsureConfigFile(modPath);
            var config = modHelper.GetJsonDataFromFile<CleanerConfig>(modPath, "config.json");
            if (conflicts.Performance) config = config with { MasterEnabled = false };

            TelemetryHub.TimingEnabled = config.Telemetry.TimingEnabled;
            config = config with
            {
                Server = LiveConfig.Sanitize(config.Server ?? new ServerToggles()),
                Debug = LiveConfig.Sanitize(config.Debug ?? new DebugOptions()),
            };
            LiveConfig.Init(config, Path.Combine(modPath, "config.json"), logger);
            LiveConfig.RouteStatus = "ok (/tushonka-cleaner/performance/config/get|set)";

            if (conflicts.Performance)
            {
                logger.Warning(LegacyServerGuard.Warning);
                return Task.CompletedTask;
            }

            // The server runs with SPT_Runtime as its working directory; fall back to the mod
            // folder if that ever stops being true.
            var userLogs = Path.Combine(Directory.GetCurrentDirectory(), "user", "logs");
            DebugControl.Init(
                Directory.Exists(userLogs) ? Path.Combine(userLogs, "CompoundingPerf") : Path.Combine(modPath, "logs"),
                message => logger.Info(message));
            AppDomain.CurrentDomain.ProcessExit += (_, _) => DebugControl.OnShutdown();

#if BENCH
            // Dev benchmark builds only: server-side GC/counter sampler. Runs on BOTH
            // sides of an A/B (it tags each line with masterEnabled), so start it
            // before the master-switch bail below.
            BenchRecorder.Start(logger, config.MasterEnabled);
#endif

            // Master A/B switch. Unlike 4.0 this now also decides whether the patches are
            // installed at all: a Harmony patch cannot be uninstalled per-feature the way
            // a DI override could be left inert, and every feature keeps its own runtime
            // kill-switch anyway.
            if (!config.MasterEnabled)
            {
                logger.Warning("[CompoundingPerf] MASTER SWITCH OFF — no patches installed (benchmark baseline mode). Flip MasterEnabled to true to re-enable.");
                DebugControl.Apply(config.Debug, config);
                DebugLog.Write("startup", "MasterEnabled=false — no patches installed, F12 switches have no effect until it is true again");
                return Task.CompletedTask;
            }

            var harmony = new Harmony(ModGuid);

            FastCompression.Apply(harmony, logger);                                        // S9
            IsolatedBotRandomisation.Apply(harmony, cloner, logger);                       // S12
            RequestTracker.Apply(harmony, logger);                                         // debug timing
            RaidWatcher.Apply(harmony, logger);                                            // debug raid summaries
            BotGenerationTracker.Apply(harmony);                                           // debug only (gated at runtime)

            // Every switch is read at call time, so the F12 menu can flip any of them later.
            LiveConfig.ApplyAtStartup(config);

            logger.Success("[CompoundingPerf] server-side features loaded");
        }
        catch (Exception ex)
        {
            logger.Error($"[CompoundingPerf] failed to load: {ex}");
            DebugLog.Write("startup", $"LOAD FAILED: {ex}");
        }

        return Task.CompletedTask;
    }
}
