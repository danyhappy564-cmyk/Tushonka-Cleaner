using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using Comfort.Common;
using EFT.UI;
using Newtonsoft.Json;
using Tushonka.Cleaner.Client.Performance;
using Tushonka.Cleaner.SceneCleanup;
using UnityEngine;

namespace Tushonka.Cleaner.Client
{
    public partial class CleanerPlugin
    {
        private const string LanguageAuto = "자동 · Auto";
        private static readonly string[] ServerLabels = { "권장 · Recommended", "균형 · Balanced", "끄기 · Off", "직접 설정 · Custom" };
        private static readonly string[] ServerIds = { "recommended", "balanced", "off", "custom" };
        private ConfigEntry<bool> _onboardingDone;
        private ConfigEntry<string> _serverLevel;
        private ConfigEntry<SceneCleanupLevel> _graphicsLevel;
        private bool _syncingSimpleUi;
        private bool _showWelcome, _chooseFeatures, _chooseServer, _chooseGraphics;
        private string _lastGameLanguage;
        private long _raidFreedGc, _raidFreedTrim, _raidFreedAssets;
        private int _raidGeneration;
        private GUIStyle _welcomeTitle, _welcomeText, _welcomeButton, _welcomeBox;
        private Rect _welcomeRect;
        private Texture2D _welcomeFill;
        private GUI.WindowFunction _welcomeDrawer;

        private void InitializeSimpleUi()
        {
            _onboardingDone = Config.Bind(InternalSection, "Onboarding completed", false,
                new ConfigDescription("First setup completed.", null, new ConfigurationManagerAttributes { Browsable = false }));
            _serverLevel = Bind(ModeSection, ModeCategory, "Server level", "서버 단계", ServerLabels[2],
                "서버 최적화 단계를 고릅니다. / Choose the server optimization level.", new AcceptableValueList<string>(ServerLabels), 96);
            Bind(ModeSection, ModeCategory, "Quick setup", "빠른 설정", "",
                "권장을 고르거나 기능마다 단계를 고릅니다. / Pick Recommended or set each feature.", null, 98, DrawQuickSetup);
            Bind(ModeSection, ModeCategory, "Open web", "웹 설정 열기", "",
                "자세한 설정은 웹에서 합니다. / Open web settings for the details.", null, 94, DrawOpenWeb);
            _graphicsLevel = Config.Bind("Graphics", "Level", SceneCleanupLevel.Off,
                new ConfigDescription("그래픽 단계 / Graphics level", null, new ConfigurationManagerAttributes()));
            _graphicsLevel.SettingChanged += (_, __) =>
            {
                if (!_conflicts.Graphics) _sceneCleanup.SetLevel(_graphicsLevel.Value);
            };
            _serverLevel.SettingChanged += (_, __) =>
            {
                if (!_syncingSimpleUi)
                {
                    int i = Array.IndexOf(ServerLabels, _serverLevel.Value);
                    if (i >= 0 && i < 3) SetServerLevel(ServerIds[i]);
                }
            };

            // 서버 첫 응답보다 먼저 지정합니다. 응답이 이 값을 덮어쓰지 않도록 메뉴에도 상태를 전달합니다.
            ServerSettingsMenu.Instance?.SetSetupPending(!_onboardingDone.Value);
            if (!_onboardingDone.Value)
            {
                _preset.Value = PresetAuto;
                ApplyPreset(PresetAuto);
                _graphicsLevel.Value = SceneCleanupLevel.Off;
                SetServerLevel("off");
            }
            foreach (var key in Config.Keys.ToArray())
            {
                var entry = Config[key];
                var attributes = entry.Description.Tags?.OfType<ConfigurationManagerAttributes>().FirstOrDefault();
                if (attributes != null) attributes.Browsable = key.Section == ModeSection || entry == _diagHotkey || entry == _graphicsLevel;
            }
            ApplyLanguage();
            RefreshSimpleUi();
        }

        private void PrepareGraphicsUi()
        {
            void Flag(string section, string key) => Config.Bind(section, key, true,
                new ConfigDescription("그래픽 세부 설정 / Graphics details", null, new ConfigurationManagerAttributes { Browsable = false }));
            foreach (var key in new[] { "Litter", "Rubble", "Casings", "Food", "Puddles", "Glass", "Decals" }) Flag("Graphics.Categories", key);
            foreach (var key in new[] { "ReduceParticles", "SoftParticles", "SoftVegetation", "RealtimeReflections", "Shadows", "PixelLights", "Lod", "ShellCap", "OtherMuzzleShadows" }) Flag("Graphics.Tuning", key);
            Config.Bind("Graphics.Advanced", "StartDelaySeconds", 10f, new ConfigDescription("검사 대기 시간(초) / Start delay (s)", new AcceptableValueRange<float>(0, 120), new ConfigurationManagerAttributes { Browsable = false }));
            Config.Bind("Graphics.Advanced", "FrameBudgetMs", 2f, new ConfigDescription("프레임당 작업 시간(ms) / Work per frame (ms)", new AcceptableValueRange<float>(.1f, 4), new ConfigurationManagerAttributes { Browsable = false }));
            Config.Bind("Graphics.Advanced", "HideDistanceMeters", 80f, new ConfigDescription("가리는 거리(m) / Hide distance (m)", new AcceptableValueRange<float>(20, 300), new ConfigurationManagerAttributes { Browsable = false }));
            Config.Bind("Graphics.Advanced", "MaxVisibilityChangesPerFrame", 24, new ConfigDescription("한 프레임에 바꾸는 개수 / Changes per frame", new AcceptableValueRange<int>(1, 64), new ConfigurationManagerAttributes { Browsable = false }));
            _graphicsLevel = Config.Bind("Graphics", "Level", SceneCleanupLevel.Off,
                new ConfigDescription("작은 장식을 가리는 정도를 고릅니다. / Choose how much small clutter to hide.", null,
                    new ConfigurationManagerAttributes { Browsable = true, Category = ModeCategory, DispName = "그래픽 단계", Order = 95,
                        ObjToStr = GraphicsLabel }));
        }

        private static string GraphicsLabel(object value) => value.ToString() switch
        {
            "LightTouch" => Loc.L("간단하게만 가림", "Light touch"), "Balanced" => Loc.L("조금 더 가림", "Hide a bit more"),
            "Max" => Loc.L("많이 가림", "Hide more clutter"), _ => Loc.L("끔", "Off"),
        };

        private void SetServerLevel(string level)
        {
            if (_conflicts.Performance) return;
            ServerSettingsMenu.Instance?.ApplyStage(ServerLevelPolicy.Get(level));
            RefreshSimpleUi();
        }

        private void QuickSetup(bool on)
        {
            _preset.Value = on ? PresetAuto : PresetOff;
            ApplyPreset(_preset.Value);
            if (on) { _trimEnabled.Value = true; _gcGrowthGb.Value = 1.5f; _gcSliceMs.Value = 2f; }
            SetServerLevel(on ? "recommended" : "off");
            _graphicsLevel.Value = on && !_conflicts.Graphics ? SceneCleanupLevel.Balanced : SceneCleanupLevel.Off;
        }

        private void RefreshSimpleUi()
        {
            if (_serverLevel == null) return;
            _syncingSimpleUi = true;
            try
            {
                string id = ServerSettingsMenu.Instance?.CurrentStage ?? "off";
                _serverLevel.Value = ServerLabels[Array.IndexOf(ServerIds, id)];
            }
            finally { _syncingSimpleUi = false; }
        }

        private void DrawQuickSetup(ConfigEntryBase entry)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Loc.L("권장", "Recommended"))) QuickSetup(true);
            if (GUILayout.Button(Loc.L("끄기", "Off"))) QuickSetup(false);
            GUILayout.EndHorizontal();
        }
        private void DrawOpenWeb(ConfigEntryBase entry)
        {
            if (GUILayout.Button(Loc.L("웹 설정 열기", "Open web settings"))) OpenWebPage();
        }

        private string BuildUiState() => JsonConvert.SerializeObject(new
        {
            memoryMode = _preset.Value == PresetAuto ? "game" : _preset.Value == PresetQuick ? "check" : _preset.Value == PresetDeep ? "analysis" :
                _preset.Value == PresetLeak ? "leak" : _preset.Value == PresetOff ? "off" : "custom",
            serverLevel = ServerSettingsMenu.Instance?.CurrentStage ?? "off",
            graphicsLevel = GraphicsLevelPolicy.FromStored(_graphicsLevel?.Value.ToString() ?? "Off"),
            language = _language.Value == LanguageKorean ? "ko" : _language.Value == LanguageEnglish ? "en" : "auto",
            resolvedLanguage = Loc.En ? "en" : "ko",
            serverReady = CompoundingPerfReady, serverConnected = _performance.Connected,
            serverStatus = _performance.Status, serverResult = _performance.LastResult,
            memoryAvailable = _ramEnabled, graphicsAvailable = !_conflicts.Graphics,
            onboardingDone = _onboardingDone?.Value ?? false,
        });

        private string ApplyUiSetting(string key, string value)
        {
            switch (key)
            {
                case "memoryMode":
                    if (!_ramEnabled) throw new InvalidOperationException(Loc.En ? LegacyConflicts.LegacyWarningEn : LegacyConflicts.LegacyWarningKo);
                    string mode = value switch { "game" => PresetAuto, "check" => PresetQuick, "analysis" => PresetDeep, "leak" => PresetLeak, "off" => PresetOff, _ => throw new ArgumentException("Unknown mode") };
                    _preset.Value = mode; ApplyPreset(mode); break;
                case "serverLevel": SetServerLevel(value); break;
                case "graphicsLevel":
                    if (_conflicts.Graphics) throw new InvalidOperationException(Loc.En ? LegacyConflicts.LegacyWarningEn : LegacyConflicts.LegacyWarningKo);
                    _graphicsLevel.Value = (SceneCleanupLevel)Enum.Parse(typeof(SceneCleanupLevel), GraphicsLevelPolicy.ToStored(value)); break;
                case "quick": QuickSetup(value == "recommended"); break;
                default: throw new ArgumentException("Unknown setting");
            }
            return "{\"ok\":true,\"state\":" + BuildUiState() + "}";
        }

        private static string ReadGameLanguage()
        {
            try
            {
                // EFT는 게임 설정을 SettingsManager 싱글턴에 보관합니다. 접근은 주 스레드에서만 합니다.
                var assembly = typeof(EFT.GameWorld).Assembly;
                Type type = assembly.GetType("EFT.Settings.SettingsManager");
                if (type == null) return null;
                object current = typeof(Singleton<>).MakeGenericType(type).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                foreach (string name in new[] { "Game", "Settings", "Language", "Value" })
                {
                    if (current == null) return null;
                    if (current is string text) return text;
                    var t = current.GetType();
                    current = t.GetProperty(name)?.GetValue(current) ?? t.GetField(name)?.GetValue(current);
                }
                return current?.ToString();
            }
            catch { return null; }
        }

        private void TickSimpleUi()
        {
            string game = ReadGameLanguage();
            if (game != _lastGameLanguage) { _lastGameLanguage = game; ApplyLanguage(); }
            RefreshSimpleUi();
            if (_onboardingDone.Value || _showWelcome || _inGame) return;
            if (MonoBehaviourSingleton<MenuUI>.Instantiated && MonoBehaviourSingleton<MenuUI>.Instance.isActiveAndEnabled)
                _showWelcome = true;
        }

        private void CompleteWelcome(bool recommended, bool later)
        {
            if (recommended) QuickSetup(true);
            else if (!later)
            {
                SetServerLevel(_chooseServer ? "recommended" : "off");
                _graphicsLevel.Value = _chooseGraphics && !_conflicts.Graphics ? SceneCleanupLevel.Balanced : SceneCleanupLevel.Off;
            }
            _onboardingDone.Value = true;
            ServerSettingsMenu.Instance?.SetSetupPending(false);
            Config.Save();
            _showWelcome = false;
        }

        private void DrawWelcome()
        {
            if (!_showWelcome || _inGame) return;
            if (_welcomeTitle == null)
            {
                _welcomeTitle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, wordWrap = true };
                _welcomeText = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                _welcomeButton = new GUIStyle(GUI.skin.button) { fontSize = 17, fixedHeight = 42 };
                _welcomeFill = new Texture2D(1, 1);
                _welcomeFill.SetPixel(0, 0, new Color(0.09f, 0.1f, 0.11f, 1f));
                _welcomeFill.Apply();
                _welcomeBox = new GUIStyle(GUI.skin.window) { padding = new RectOffset(24, 24, 32, 24) };
                _welcomeBox.normal.background = _welcomeFill;
                _welcomeBox.onNormal.background = _welcomeFill;
            }
            float width = Math.Min(820, Screen.width - 32), height = Math.Min(680, Screen.height - 24);
            _welcomeRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            if (_welcomeDrawer == null) _welcomeDrawer = DrawWelcomeContent;
            GUI.ModalWindow(697703, _welcomeRect, _welcomeDrawer, "Tushonka Cleaner", _welcomeBox);
        }

        private void DrawWelcomeContent(int id)
        {
            GUILayout.Label(Loc.L("처음 한 번만 안내합니다.", "A one-time setup."), _welcomeText);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Loc.L("언어: 자동", "Language: Auto"))) _language.Value = LanguageAuto;
            if (GUILayout.Button("한국어")) _language.Value = LanguageKorean;
            if (GUILayout.Button("English")) _language.Value = LanguageEnglish;
            GUILayout.EndHorizontal();
            GUILayout.Label(Loc.L("Tushonka Cleaner를 설정합니다.", "Set up Tushonka Cleaner."), _welcomeTitle);
            GUILayout.Label(Loc.L("권장을 누르면 세 기능을 함께 켭니다. 직접 고르면 원하는 기능만 켭니다.", "Use Recommended to turn on all three features, or choose the ones you want."), _welcomeText);
            GUILayout.Space(10);
            GUILayout.Label(Loc.L("01  메모리 및 VGA — 게임 중 메모리를 정리하고 부족할 때 알립니다.", "01  Memory & VRAM — Clean up memory while you play and warn when it runs low."), _welcomeText);
            GUILayout.Label(Loc.L("02  서버 — 안전한 서버 최적화를 함께 켭니다.", "02  Server — Enable the safe server optimizations."), _welcomeText);
            GUILayout.Label(Loc.L("03  그래픽 — 그래픽을 조금 줄여 프레임을 올리고 작은 장식을 가립니다.", "03  Graphics — Lower graphics a little for more FPS and hide small clutter."), _welcomeText);
            GUILayout.Space(10);
            GUILayout.Label(Loc.L("자세한 설정은 웹에서 엽니다.", "Fine-tune your setup on the web."), _welcomeText);
            GUILayout.Label(_web?.Url ?? $"http://127.0.0.1:{_webPort.Value}/", _welcomeText);
            if (GUILayout.Button(Loc.L("웹 설정 열기", "Open web settings"), _welcomeButton)) OpenWebPage();
            if (_chooseFeatures)
            {
                _chooseServer = GUILayout.Toggle(_chooseServer, Loc.L("서버", "Server"), _welcomeText);
                _chooseGraphics = GUILayout.Toggle(_chooseGraphics, Loc.L("그래픽", "Graphics"), _welcomeText);
                if (GUILayout.Button(Loc.L("선택한 기능만 켜기", "Enable my choices"), _welcomeButton)) CompleteWelcome(false, false);
                if (GUILayout.Button(Loc.L("돌아가기", "Back"))) _chooseFeatures = false;
            }
            else
            {
                if (GUILayout.Button(Loc.L("권장으로 모두 켜기", "Enable all recommended"), _welcomeButton)) CompleteWelcome(true, false);
                if (GUILayout.Button(Loc.L("직접 고르기", "Let me choose"), _welcomeButton)) _chooseFeatures = true;
                if (GUILayout.Button(Loc.L("나중에", "Not now"), _welcomeButton)) CompleteWelcome(false, true);
            }
            GUILayout.Label(Loc.L("나중에를 눌러도 다시 묻지 않습니다. 이후 F12에서 웹 설정을 엽니다.", "Not now dismisses this prompt for good. Open web settings from F12 later."), _welcomeText);
        }
    }
}
