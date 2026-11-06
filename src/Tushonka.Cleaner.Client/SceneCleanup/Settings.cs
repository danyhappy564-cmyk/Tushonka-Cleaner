// SPDX-License-Identifier: GPL-3.0-only
using BepInEx.Configuration;

namespace Tushonka.Cleaner.SceneCleanup
{
    internal sealed class CleanupSettings
    {
        internal readonly ConfigEntry<SceneCleanupLevel> Level;
        internal readonly ConfigEntry<float> Delay, Budget, Distance;
        internal readonly ConfigEntry<bool>[] Categories = new ConfigEntry<bool>[7];
        internal readonly ConfigEntry<bool> Particles, SoftParticles, SoftVegetation, Reflections, Shadows, Lights, Lod, ShellCap, MuzzleShadows;
        internal readonly ConfigEntry<int> ChangesPerFrame;
        internal CleanupSettings(ConfigFile config)
        {
            Level=config.Bind("Graphics","Level",SceneCleanupLevel.Off,"그래픽 단계: 끔 / 간단하게만 가림 / 균형 / 최대. 데칼 변경은 다음 레이드부터 적용됩니다. / Graphics: Off / LightTouch / Balanced / Max. Decal changes apply next raid.");
            Delay=config.Bind("Graphics.Advanced","StartDelaySeconds",10f,new ConfigDescription("레이드 시작 후 검사 대기 시간(초). / Delay after raid starts (seconds).",new AcceptableValueRange<float>(0,120)));
            Budget=config.Bind("Graphics.Advanced","FrameBudgetMs",2f,new ConfigDescription("프레임당 작업 시간 예산(ms). 단일 Unity 호출 시간은 실측이 필요합니다. / Work budget per frame in ms; individual Unity calls require profiling.",new AcceptableValueRange<float>(.1f,4f)));
            Distance=config.Bind("Graphics.Advanced","HideDistanceMeters",80f,new ConfigDescription("간단하게만 가림의 기준 거리(m). 현재 주 카메라와 스코프 시야각에 따라 늘어납니다. / LightTouch distance in meters, expanded with current main/optic FOV.",new AcceptableValueRange<float>(20,300)));
            ChangesPerFrame=config.Bind("Graphics.Advanced","MaxVisibilityChangesPerFrame",24,new ConfigDescription("조준 시 한 프레임에 변경할 렌더러 수 상한. / Maximum renderer visibility changes per frame when aiming.",new AcceptableValueRange<int>(1,64)));
            string[] keys={"Litter","Rubble","Casings","Food","Puddles","Glass","Decals"};
            string[] ko={"바닥 쓰레기","잔해 더미","맵에 고정된 탄피","먹을 수 없는 장식 음식","물웅덩이","깨진 유리 조각","얼룩·낙서 (다음 레이드부터 적용)"};
            for(int i=0;i<7;i++) Categories[i]=config.Bind("Graphics.Categories",keys[i],true,ko[i]+" / Enable "+keys[i]+(i==6?" (next raid only).":"."));
            Particles=Flag(config,"ReduceParticles","파티클 배출 예산 줄이기","Reduce the particle raycast budget");
            SoftParticles=Flag(config,"SoftParticles","부드러운 파티클 끄기","Disable soft particles");
            SoftVegetation=Flag(config,"SoftVegetation","부드러운 식생 끄기","Disable soft vegetation");
            Reflections=Flag(config,"RealtimeReflections","실시간 반사 끄기","Disable realtime reflections");
            Shadows=Flag(config,"Shadows","그림자 품질 줄이기","Reduce shadow quality");
            Lights=Flag(config,"PixelLights","픽셀 조명 수 줄이기","Reduce pixel light count");
            Lod=Flag(config,"Lod","먼 모델 단순화(많이 가림에서만 1.5배 빠르게)","Simplify distant models (Max only, 1.5x sooner)");
            ShellCap=config.Bind("Graphics.Tuning","ShellCap",true,"버려진 탄피 수 상한. 끔을 뺀 모든 단계에서 적용하며 끄면 게임 원래 동작을 사용합니다. / Limit ejected used shell count. Applies at every level except Off; disable to use original behaviour.");
            MuzzleShadows=Flag(config,"OtherMuzzleShadows","다른 사람의 총구 화염 그림자 끄기","Disable other players' muzzle light shadows");
        }
        private static ConfigEntry<bool> Flag(ConfigFile c,string key,string ko,string en)
        { return c.Bind("Graphics.Tuning",key,true,ko+". 균형·최대에서만 적용하며 끄면 원래 설정을 사용합니다. / "+en+". Balanced/Max only; disable to use original settings."); }
    }
}
