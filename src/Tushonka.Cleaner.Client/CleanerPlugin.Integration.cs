using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Tushonka.Cleaner.Client.Graphics;
using Tushonka.Cleaner.Client.Performance;
using UnityEngine;

namespace Tushonka.Cleaner.Client
{
    public partial class CleanerPlugin
    {
        public const string ModGuid = "com.zzap.tushonka.cleaner";
        public const string ModName = "Tushonka Cleaner";
        public const string ModVersion = "3.0.0";

        private readonly PerformanceModule _performance = new PerformanceModule();
        private readonly ISceneCleanup _sceneCleanup = new ModuleSceneCleanup();
        private LegacyConflicts _conflicts = new LegacyConflicts();
        private bool _ramEnabled;
        private string _legacyWarningKo = "";
        private string _legacyWarningEn = "";
        private Dictionary<string, string> _legacyValues;
        private int _migratedCount;

        private void PrepareIntegration()
        {
            _conflicts = LegacyConflicts.Detect(Chainloader.PluginInfos.Keys);
            _ramEnabled = !_conflicts.Memory;
            if (_conflicts.Any)
            {
                var guids = string.Join(", ", _conflicts.Guids);
                _legacyWarningKo = "Tushonka Cleaner: 이전 모드가 함께 설치되어 겹치는 기능을 중지했습니다.\n" +
                                   "게임과 서버를 끄고 서버를 다시 켜면 이전 모드가 백업 폴더로 자동으로 옮겨집니다.\n" + guids;
                _legacyWarningEn = "Tushonka Cleaner: Old mods are still installed, so overlapping features are off.\n" +
                                   "Close the game and server, then start the server again to move them to a backup folder automatically.\n" + guids;
                Logger.LogWarning(_legacyWarningEn);
            }
            if (_conflicts.Smoothie) Logger.LogWarning(LegacyConflicts.SmoothieWarningEn);

            var oldConfig = Path.Combine(BepInEx.Paths.ConfigPath, "com.cactuspie.ramcleanerinterval.cfg");
            if (!File.Exists(Config.ConfigFilePath) && File.Exists(oldConfig))
            {
                try { _legacyValues = LegacyConfigMigration.Parse(false, File.ReadAllLines(oldConfig)); }
                catch (Exception ex) { Logger.LogWarning($"구형 RAM 설정을 읽지 못했습니다: {ex.Message}"); }
            }
        }

        private ConfigEntry<T> BindMigrated<T>(string section, string key, T defaultValue, ConfigDescription description)
        {
            var entry = Config.Bind(section, key, defaultValue, description);
            if (_legacyValues != null && LegacyConfigMigration.TryGet(_legacyValues, section, key, out var value))
            {
                try { entry.SetSerializedValue(value); _migratedCount++; }
                catch (Exception ex) { Logger.LogWarning($"설정 이전 건너뜀: {section}/{key}: {ex.Message}"); }
            }
            return entry;
        }

        private void CompleteIntegrationSettings()
        {
            if (_legacyValues != null)
            {
                Config.Save();
                Logger.LogInfo($"구형 RAM 설정 {_migratedCount}개를 같은 절·키로 이전했습니다. 원본 cfg는 보존했습니다.");
                _legacyValues = null;
            }
            if (!_ramEnabled)
            {
                // 구형 RAM 모드와 중복 실행되지 않도록 수동 실행 버튼도 숨깁니다.
                foreach (var definition in Config.Keys.ToList())
                    foreach (var tag in Config[definition].Description.Tags ?? Array.Empty<object>())
                        if (tag is ConfigurationManagerAttributes attributes) attributes.Browsable = false;
            }
            if (!_conflicts.Performance) _performance.Initialize(Config, Logger);
            PrepareGraphicsUi();
            if (!_conflicts.Graphics)
                _sceneCleanup.Init(Config, Logger, Path.GetDirectoryName(Info.Location));
            InitializeSimpleUi();
        }

        private void DrawLegacyWarning()
        {
            var width = Math.Min(Screen.width - 24, 1000);
            if (_legacyWarningKo.Length > 0)
                GUI.Box(new Rect(12, Math.Max(12, Screen.height - 120), width, 100), Loc.En ? _legacyWarningEn : _legacyWarningKo);
            // 레이드 화면을 가리지 않도록 메뉴에서만 보여 줍니다.
            if (_conflicts.Smoothie && !_inGame)
                GUI.Box(new Rect(12, Math.Max(12, Screen.height - 190), width, 60),
                    Loc.En ? LegacyConflicts.SmoothieWarningEn : LegacyConflicts.SmoothieWarningKo);
        }
    }
}
