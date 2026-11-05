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
        private readonly ISceneCleanup _sceneCleanup = new NullSceneCleanup();
        private LegacyConflicts _conflicts = new LegacyConflicts();
        private bool _ramEnabled;
        private string _legacyWarning = "";
        private Dictionary<string, string> _legacyValues;
        private int _migratedCount;

        private void PrepareIntegration()
        {
            _conflicts = LegacyConflicts.Detect(Chainloader.PluginInfos.Keys);
            _ramEnabled = !_conflicts.Memory;
            if (_conflicts.Any)
            {
                _legacyWarning = "Tushonka Cleaner: 구형 모드가 함께 설치되어 겹치는 기능을 중지했습니다. 구형 모드를 삭제하십시오.\n" +
                                 string.Join(", ", _conflicts.Guids);
                Logger.LogWarning(_legacyWarning);
            }

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
            if (!_conflicts.Graphics)
                _sceneCleanup.Init(Config, Logger, Path.Combine(BepInEx.Paths.BepInExRootPath, "RamCleaner"));
        }

        private void DrawLegacyWarning()
        {
            if (_legacyWarning.Length == 0) return;
            GUI.Box(new Rect(12, Math.Max(12, Screen.height - 120), Math.Min(Screen.width - 24, 1000), 100), _legacyWarning);
        }
    }
}
