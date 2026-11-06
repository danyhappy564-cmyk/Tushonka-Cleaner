#nullable enable
// Compile-included by both the server project (net10.0) and the client project
// (netstandard2.1). Avoid `required` so both target frameworks accept it — use defaults.

namespace Tushonka.Cleaner;

public record CleanerConfig
{
    /// <summary>One-switch A/B toggle: when false, EVERY optimization is disabled (server
    /// and client) regardless of individual flags — but the frame-stats recorder keeps
    /// running, so with/without benchmark runs are a single config flip apart.</summary>
    public bool MasterEnabled { get; set; } = true;

    public ServerToggles Server { get; set; } = new();
    public ClientToggles Client { get; set; } = new();
    public DebugOptions Debug { get; set; } = new();
    public TelemetryOptions Telemetry { get; set; } = new();
    public CompatOptions Compat { get; set; } = new();
}

public record ServerToggles
{
    public FastCompressionOptions           FastCompression          { get; set; } = new();
    public IsolatedBotRandomisationOptions  IsolatedBotRandomisation { get; set; } = new();

    // Retired in 2.0 because SPT 4.1 does the job itself, verified against the 4.1.5
    // server assembly rather than assumed:
    //   ProfileSaveDebouncer (S1) - SaveProfileAsync now takes a per-profile SemaphoreSlim,
    //                               so saves for one profile no longer overlap.
    //   ResponseCache (S2)        - the heavy endpoints (items, globals, handbook,
    //                               customization, hideout areas/recipes) now return
    //                               StreamedJsonBody and serialize straight to the response
    //                               stream, so there is no longer a big string to cache.
    //   ThreadSafeRandom (S6)     - RandomUtil no longer holds a shared System.Random; it
    //                               uses RandomNumberGenerator, which is thread-safe.
    //   ResponseSanitizer (S7)    - ClearString is already a single SearchValues scan over
    //                               a pooled buffer.
    //   ThreadSafeCaches (S10)    - ItemBaseClassService now guards its cache with a Lock,
    //                               HandbookHelper's lazy init is benign, and nothing inside
    //                               SPT calls ItemFilterService's blacklist mutators at all.
    // Retired in Tushonka Cleaner 1.1.0 after measuring them on 4.1.6 (numbers on the site and in docs/):
    //   RagfairCalmUpdates (S8)  - the forced flea GC is GCCollectionMode.Optimized; the runtime skipped it
    //                              every time (0 ms, no gen2), so removing it changed nothing.
    //   SaveDirtyTracking (S11)  - could drop a real save after a failed one; risk with no measured gain.
    //   CalmNotifier (S13)       - the client uses the websocket; the long-poll was never called.
    //   RaidStartGc (S15)        - 0.3 s off a 170 s raid load for 1.2 GB more server memory in raid.
    //   PostRaidCleanup (S16)    - added a second forced GC that ignored other FIKA players.
    // S14 (FastRouteDispatch) was REMOVED before release: memoizing url→router
    // resolution changed the /launcher/server/connect response under FIKA (raw
    // 0.0.0.0 backendUrl → game cannot connect). Root cause not fully explained,
    // which by itself disqualifies a behavior-neutrality-critical feature.
}

public record IsolatedBotRandomisationOptions
{
    // S12: fixes a verified vanilla bug — night-raid equipment modifiers are written
    // into SHARED bot config: they compound per generated bot, persist across raids
    // until restart, and race across vanilla's parallel bot generation. The fix gives
    // every caller a private clone, so the modifier applies exactly once per bot.
    public bool Enabled { get; set; } = true;
}


public record DebugOptions
{
    // New in 2.1: a separate verification log, user/logs/CompoundingPerf/CompoundingPerf-debug-*.log.
    // Startup self-check (did each patch land), every forced collect it touched, raid
    // start/end timing, slow requests, bot generation timing, and a periodic memory/GC/
    // feature-counter summary. Off by default; it adds one timestamp and one task
    // continuation per HTTP request while on.
    public bool Enabled { get; set; } = false;

    /// <summary>Minutes between periodic summary lines. 0 turns the periodic summary off
    /// (raid start/end summaries are still written).</summary>
    public int SummaryIntervalMinutes { get; set; } = 5;

    /// <summary>Requests that take at least this long (receive + route + compress + send)
    /// are logged individually.</summary>
    public int SlowRequestMs { get; set; } = 250;

    /// <summary>How many debug log files to keep; older ones are deleted at startup.</summary>
    public int KeepFiles { get; set; } = 10;
}

public record FastCompressionOptions
{
    // S9: vanilla compresses every JSON response at CompressionLevel.SmallestSize —
    // zlib's slowest setting. Measured on 4.1.6 over nine main endpoints: SmallestSize 636 ms / 4.9 MB,
    // Optimal 440 ms / 4.9 MB, Fastest 351 ms / 7.8 MB. Optimal is the default: same size, so a remote
    // FIKA client loses nothing; Fastest stays available for a server on the same PC.
    public bool Enabled { get; set; } = true;

    /// <summary>One of: Fastest, Optimal, SmallestSize, NoCompression. Unrecognized
    /// values fall back to Optimal.</summary>
    public string Level { get; set; } = "Optimal";
}

public record ClientToggles
{
    // The in-raid log suppressor (C4) was removed in 1.3: its benefit never survived
    // measurement on real setups, and suppressing other mods' in-raid logs breaks the
    // ecosystem's debugging currency. FrameStats is BENCH-dev-build-only.
    public FrameStatsOptions FrameStats { get; set; } = new();
}

public record FrameStatsOptions
{
    // C6: per-raid frame-time benchmark recorder. One float write per frame while in
    // raid (no allocation, no measurable cost); on raid end, appends a stats line
    // (avg FPS, 1%/0.1% lows, hitch counts, worst spike) to
    // SPT/user/logs/CompoundingPerf-framestats.jsonl. Deliberately NOT gated by
    // MasterEnabled so with/without comparisons measure both sides.
    public bool Enabled { get; set; } = true;

    /// <summary>Leading seconds of each raid discarded from the stats — the spawn-in /
    /// asset-streaming window produces multi-second frames that say nothing about
    /// gameplay performance and would dominate the lows.</summary>
    public double WarmupSkipSeconds { get; set; } = 20;
}




/// <summary>Retired in 2.1. The 2.0 port kept these keys but nothing ever read them except
/// <see cref="TimingEnabled"/>, so turning telemetry on did nothing. <see cref="DebugOptions"/>
/// replaces it. Kept so an old config.json still deserializes.</summary>
public record TelemetryOptions
{
    public bool Enabled { get; set; } = false;
    public int DumpEveryNRaids { get; set; } = 10;
    public bool DumpOnRaidEnd { get; set; } = true;
    public bool DumpOnServerShutdown { get; set; } = true;

    /// <summary>Stopwatch timing on hot paths is opt-in because the measurement itself
    /// has overhead. Counters are always cheap.</summary>
    public bool TimingEnabled { get; set; } = false;
}

public record CompatOptions
{
    public bool AutoDisableOnConflict { get; set; } = true;
    public bool Verbose { get; set; } = true;
}

/// <summary>
/// What the server's settings route (<c>/tushonka-cleaner/performance/config/get</c> and <c>/set</c>)
/// answers, for the client's F12 menu. Shared by both sides like the rest of this file.
/// </summary>
public record ServerSettingsResponse
{
    public bool Ok { get; set; } = true;
    public string? Error { get; set; }
    public string ServerVersion { get; set; } = "";

    /// <summary>When false no patches were installed at startup, so the switches below do
    /// nothing until config.json's MasterEnabled is true again and the server restarts.</summary>
    public bool MasterEnabled { get; set; } = true;

    public ServerToggles? Server { get; set; }
    public DebugOptions? Debug { get; set; }

    /// <summary>What the last /set changed ("" when nothing did), and whether it reached config.json.</summary>
    public string? Changes { get; set; }
    public bool Saved { get; set; }

    public long HeapMb { get; set; }
    public long CommittedMb { get; set; }
    public long ProcessMb { get; set; }
    public string? DebugLogPath { get; set; }
}
