// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Tushonka.Cleaner.SceneCleanup
{
    /// <summary>호출은 Unity 주 스레드에서 수행합니다. dataDir에는 scene-rules.json과 선택적 제외 파일을 둡니다.</summary>
    public static class SceneCleanupModule
    {
        internal static RuntimeState State;
        private static GameHooks hooks;
        public static void Init(ConfigFile config,ManualLogSource log,string dataDir)
        {
            if(config==null || log==null || dataDir==null) throw new ArgumentNullException();
            Shutdown();
            var rules=RuleBook.Load(File.ReadAllText(Path.Combine(dataDir,"scene-rules.json")));
            string exclude=Path.Combine(dataDir,"tushonka-exclude.txt");
            var exclusions=new ExclusionSet(File.Exists(exclude)?File.ReadAllLines(exclude):new string[0]);
            State=new RuntimeState(config,log,new CleanupSettings(config),rules,exclusions);
            hooks=new GameHooks();
            try { hooks.Install(); }
            catch { Shutdown(); throw; }
            log.LogInfo("그래픽 모듈 초기화. 데칼 변경은 다음 레이드부터 적용됩니다. / Graphics initialized; decal changes apply next raid.");
        }
        public static void Tick() { State?.Tick(); }
        public static void SetLevel(SceneCleanupLevel level)
        {
            if(level<SceneCleanupLevel.Off || level>SceneCleanupLevel.Max) throw new ArgumentOutOfRangeException(nameof(level));
            State?.SetLevel(level);
        }
        public static SceneCleanupStatus GetStatus() { return State==null?default(SceneCleanupStatus):State.Status; }
        public static void Shutdown()
        {
            try { if(hooks!=null) hooks.Remove(); }
            finally
            {
                hooks=null;
                if(State!=null) { State.Shutdown(); State=null; }
            }
        }
    }
}
