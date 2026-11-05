using BepInEx.Configuration;
using BepInEx.Logging;

namespace Tushonka.Cleaner.Client.Graphics;

/// <summary>그래픽 모듈을 연결하기 전까지 게임 상태를 변경하지 않습니다.</summary>
public sealed class NullSceneCleanup : ISceneCleanup
{
    public void Init(ConfigFile config, ManualLogSource log, string dataDir) { }
    public void Tick() { }
    public void SetLevel(SceneCleanupLevel level) { }
    public string GetStatus() => "Off";
    public void Shutdown() { }
}
