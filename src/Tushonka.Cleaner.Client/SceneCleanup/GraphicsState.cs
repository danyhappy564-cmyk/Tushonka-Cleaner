// SPDX-License-Identifier: GPL-3.0-only
using System;
using UnityEngine;

namespace Tushonka.Cleaner.SceneCleanup
{
    // LOD 거리 배율은 많이 가림에서만 원래 값의 1/1.5로 낮춥니다. 고정값으로 누르면 벽과 바닥이 사라집니다(1.0.0의 0.8·0.5).
    // 그림자 해상도·거리는 읽지도 쓰지도 않습니다.
    // 레이드 중 게임 설정 화면에서 값을 바꾸면 그 값을 새 원래 값으로 받아들입니다. 마지막으로 쓴 값과 다르면 다른 쪽이 바꾼 것입니다.
    // LOD는 받아들이지 않습니다. 게임의 CameraLodBiasController가 그리는 동안 값을 잠시 바꿉니다.
    internal sealed class GraphicsState
    {
        internal const int SlotCount=8;
        private const int MinParticleBudget=4, MaxCascades=2;
        private const float MaxLodDivider=1.5f;
        private bool captured;
        private int particleBudget,pixelLights,cascades;
        private float lodBias;
        private bool softParticles,softVegetation,reflections;
        private ShadowQuality shadows;
        private int lastBudget,lastLights,lastCascades;
        private bool lastSoftParticles,lastSoftVegetation,lastReflections;
        private ShadowQuality lastShadows;
        internal void Capture()
        {
            if(captured) return;
            particleBudget=QualitySettings.particleRaycastBudget; pixelLights=QualitySettings.pixelLightCount;
            softParticles=QualitySettings.softParticles; softVegetation=QualitySettings.softVegetation;
            reflections=QualitySettings.realtimeReflectionProbes; shadows=QualitySettings.shadows;
            cascades=QualitySettings.shadowCascades; lodBias=QualitySettings.lodBias; captured=true;
            lastBudget=particleBudget; lastLights=pixelLights; lastCascades=cascades; lastSoftParticles=softParticles;
            lastSoftVegetation=softVegetation; lastReflections=reflections; lastShadows=shadows;
        }
        // 한 Tick에 한 속성만 적용하여 설정 변경을 분산합니다.
        internal void Apply(int slot,CleanupSettings s,SceneCleanupLevel level)
        {
            if(!captured) return;
            bool active=level>=SceneCleanupLevel.Balanced; bool max=level==SceneCleanupLevel.Max;
            switch(slot)
            {
                case 0: Adopt(ref particleBudget,lastBudget,QualitySettings.particleRaycastBudget); lastBudget=QualitySettings.particleRaycastBudget=active&&s.Particles.Value?Math.Max(particleBudget/(max?4:2),MinParticleBudget):particleBudget; break;
                case 1: Adopt(ref softParticles,lastSoftParticles,QualitySettings.softParticles); lastSoftParticles=QualitySettings.softParticles=active&&s.SoftParticles.Value?false:softParticles; break;
                case 2: Adopt(ref softVegetation,lastSoftVegetation,QualitySettings.softVegetation); lastSoftVegetation=QualitySettings.softVegetation=active&&s.SoftVegetation.Value?false:softVegetation; break;
                case 3: Adopt(ref reflections,lastReflections,QualitySettings.realtimeReflectionProbes); lastReflections=QualitySettings.realtimeReflectionProbes=active&&s.Reflections.Value?false:reflections; break;
                case 4: Adopt(ref shadows,lastShadows,QualitySettings.shadows); WriteShadows(max&&s.Shadows.Value&&shadows==ShadowQuality.All?ShadowQuality.HardOnly:shadows); break;
                case 5: Adopt(ref cascades,lastCascades,QualitySettings.shadowCascades); lastCascades=QualitySettings.shadowCascades=max&&s.Shadows.Value?Mathf.Min(cascades,MaxCascades):cascades; break;
                case 6: Adopt(ref pixelLights,lastLights,QualitySettings.pixelLightCount); lastLights=QualitySettings.pixelLightCount=max&&s.Lights.Value?Math.Max(pixelLights/2,0):pixelLights; break;
                case 7: QualitySettings.lodBias=max&&s.Lod.Value?lodBias/MaxLodDivider:lodBias; break;
            }
        }
        internal void Restore()
        {
            if(!captured) return;
            Adopt(ref particleBudget,lastBudget,QualitySettings.particleRaycastBudget); QualitySettings.particleRaycastBudget=particleBudget;
            Adopt(ref pixelLights,lastLights,QualitySettings.pixelLightCount); QualitySettings.pixelLightCount=pixelLights;
            Adopt(ref softParticles,lastSoftParticles,QualitySettings.softParticles); QualitySettings.softParticles=softParticles;
            Adopt(ref softVegetation,lastSoftVegetation,QualitySettings.softVegetation); QualitySettings.softVegetation=softVegetation;
            Adopt(ref reflections,lastReflections,QualitySettings.realtimeReflectionProbes); QualitySettings.realtimeReflectionProbes=reflections;
            Adopt(ref shadows,lastShadows,QualitySettings.shadows); WriteShadows(shadows);
            Adopt(ref cascades,lastCascades,QualitySettings.shadowCascades); QualitySettings.shadowCascades=cascades;
            QualitySettings.lodBias=lodBias; captured=false;
        }
        private static void Adopt<T>(ref T original,T last,T current) where T:struct,IEquatable<T> { if(!current.Equals(last)) original=current; }
        private static void Adopt(ref ShadowQuality original,ShadowQuality last,ShadowQuality current) { if(current!=last) original=current; }
        // 게임 값이 Disable이면 이 값을 바꾼 적이 없으므로 쓸 일도 없습니다.
        private void WriteShadows(ShadowQuality value) { if(value!=ShadowQuality.Disable) QualitySettings.shadows=value; lastShadows=QualitySettings.shadows; }
        internal void ForgetSnapshot() { captured=false; }
    }
}
