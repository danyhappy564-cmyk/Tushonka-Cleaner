// SPDX-License-Identifier: GPL-3.0-only
using System;
using UnityEngine;

namespace Tushonka.Cleaner.SceneCleanup
{
    // LOD 거리 배율은 많이 가림에서만 원래 값의 1/1.5로 낮춥니다. 고정값으로 누르면 벽과 바닥이 사라집니다(1.0.0의 0.8·0.5).
    // 그림자 해상도·거리는 읽지도 쓰지도 않습니다.
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
        internal void Capture()
        {
            if(captured) return;
            particleBudget=QualitySettings.particleRaycastBudget; pixelLights=QualitySettings.pixelLightCount;
            softParticles=QualitySettings.softParticles; softVegetation=QualitySettings.softVegetation;
            reflections=QualitySettings.realtimeReflectionProbes; shadows=QualitySettings.shadows;
            cascades=QualitySettings.shadowCascades; lodBias=QualitySettings.lodBias; captured=true;
        }
        // 한 Tick에 한 속성만 적용하여 설정 변경을 분산합니다.
        internal void Apply(int slot,CleanupSettings s,SceneCleanupLevel level)
        {
            if(!captured) return;
            bool active=level>=SceneCleanupLevel.Balanced; bool max=level==SceneCleanupLevel.Max;
            switch(slot)
            {
                case 0: QualitySettings.particleRaycastBudget=active&&s.Particles.Value?Math.Max(particleBudget/(max?4:2),MinParticleBudget):particleBudget; break;
                case 1: QualitySettings.softParticles=active&&s.SoftParticles.Value?false:softParticles; break;
                case 2: QualitySettings.softVegetation=active&&s.SoftVegetation.Value?false:softVegetation; break;
                case 3: QualitySettings.realtimeReflectionProbes=active&&s.Reflections.Value?false:reflections; break;
                case 4: WriteShadows(max&&s.Shadows.Value&&shadows==ShadowQuality.All?ShadowQuality.HardOnly:shadows); break;
                case 5: QualitySettings.shadowCascades=max&&s.Shadows.Value?Mathf.Min(cascades,MaxCascades):cascades; break;
                case 6: QualitySettings.pixelLightCount=max&&s.Lights.Value?Math.Max(pixelLights/2,0):pixelLights; break;
                case 7: QualitySettings.lodBias=max&&s.Lod.Value?lodBias/MaxLodDivider:lodBias; break;
            }
        }
        internal void Restore()
        {
            if(!captured) return;
            QualitySettings.particleRaycastBudget=particleBudget; QualitySettings.pixelLightCount=pixelLights;
            QualitySettings.softParticles=softParticles; QualitySettings.softVegetation=softVegetation;
            QualitySettings.realtimeReflectionProbes=reflections; WriteShadows(shadows);
            QualitySettings.shadowCascades=cascades; QualitySettings.lodBias=lodBias; captured=false;
        }
        // 게임 값이 Disable이면 이 값을 바꾼 적이 없으므로 쓸 일도 없습니다.
        private static void WriteShadows(ShadowQuality value) { if(value!=ShadowQuality.Disable) QualitySettings.shadows=value; }
        internal void ForgetSnapshot() { captured=false; }
    }
}
