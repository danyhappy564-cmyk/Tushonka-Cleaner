using BepInEx.Configuration;
using BepInEx.Logging;
using Tushonka.Cleaner.SceneCleanup;

namespace Tushonka.Cleaner.Client.Graphics;

/// <summary>그래픽 정리 모듈의 진입점입니다.</summary>
public interface ISceneCleanup
{
    void Init(ConfigFile config, ManualLogSource log, string dataDir);
    void Tick();
    void SetLevel(SceneCleanupLevel level);
    string GetStatus();
    void Shutdown();
}
