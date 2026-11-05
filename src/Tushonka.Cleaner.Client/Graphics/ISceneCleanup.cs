using BepInEx.Configuration;
using BepInEx.Logging;

namespace Tushonka.Cleaner.Client.Graphics;

public enum SceneCleanupLevel
{
    Off,
    LightTouch,
    Balanced,
    Max
}

/// <summary>다음 단계에서 그래픽 정리 모듈을 연결하는 진입점입니다.</summary>
public interface ISceneCleanup
{
    void Init(ConfigFile config, ManualLogSource log, string dataDir);
    void Tick();
    void SetLevel(SceneCleanupLevel level);
    string GetStatus();
    void Shutdown();
}
