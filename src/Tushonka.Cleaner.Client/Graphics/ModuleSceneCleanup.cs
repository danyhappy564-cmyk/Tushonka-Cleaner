using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using Tushonka.Cleaner.SceneCleanup;

namespace Tushonka.Cleaner.Client.Graphics;

/// <summary>그래픽 모듈을 플러그인에 연결합니다. 모듈에 문제가 생기면 그래픽 기능만 끄고 게임은 계속 둡니다.</summary>
public sealed class ModuleSceneCleanup : ISceneCleanup
{
    private ManualLogSource _log;
    private bool _active;

    public void Init(ConfigFile config, ManualLogSource log, string dataDir)
    {
        _log = log;
        try
        {
            SceneCleanupModule.Init(config, log, dataDir);
            _active = true;
        }
        catch (Exception ex)
        {
            Disable("초기화", ex);
        }
    }

    public void Tick()
    {
        if (!_active) return;
        try { SceneCleanupModule.Tick(); }
        catch (Exception ex) { Disable("실행", ex); }
    }

    public void SetLevel(SceneCleanupLevel level)
    {
        if (!_active) return;
        try { SceneCleanupModule.SetLevel(level); }
        catch (Exception ex) { Disable("단계 변경", ex); }
    }

    public string GetStatus() => _active ? SceneCleanupModule.GetStatus().Level.ToString() : "Off";

    public void Shutdown()
    {
        _active = false;
        try { SceneCleanupModule.Shutdown(); }
        catch (Exception ex) { _log?.LogWarning($"그래픽 기능 종료 실패 / Graphics shutdown failed: {ex.Message}"); }
    }

    private void Disable(string step, Exception ex)
    {
        _active = false;
        _log?.LogError($"그래픽 기능을 껐습니다({step}) / Graphics turned off ({step}): {ex}");
        try { SceneCleanupModule.Shutdown(); }
        catch (Exception shutdown) { _log?.LogWarning($"그래픽 기능 복원 실패 / Graphics restore failed: {shutdown.Message}"); }
    }
}
