// SPDX-License-Identifier: GPL-3.0-only
using System;
using UnityEngine;

namespace Tushonka.Cleaner.SceneCleanup
{
    // LOD 거리 배율을 낮추면 벽과 바닥이 사라집니다. 이 배율과 그림자 해상도·거리는 읽지도 쓰지도 않습니다.
    internal sealed class GraphicsState
    {
        internal const int SlotCount=7;
        private const int MinParticleBudget=4, MaxCascades=2;
        private bool captured;
        private int particleBudget,pixelLights,cascades;
        private bool softParticles,softVegetation,reflections;
        private ShadowQuality shadows;
        internal void Capture()
        {
            if(captured) return;
            particleBudget=QualitySettings.particleRaycastBudget; pixelLights=QualitySettings.pixelLightCount;
            softParticles=QualitySettings.softParticles; softVegetation=QualitySettings.softVegetation;
            reflections=QualitySettings.realtimeReflectionProbes; shadows=QualitySettings.shadows;
            cascades=QualitySettings.shadowCascades; captured=true;
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
            }
        }
        internal void Restore()
        {
            if(!captured) return;
            QualitySettings.particleRaycastBudget=particleBudget; QualitySettings.pixelLightCount=pixelLights;
            QualitySettings.softParticles=softParticles; QualitySettings.softVegetation=softVegetation;
            QualitySettings.realtimeReflectionProbes=reflections; WriteShadows(shadows);
            QualitySettings.shadowCascades=cascades; captured=false;
        }
        // 게임 값이 Disable이면 이 값을 바꾼 적이 없으므로 쓸 일도 없습니다.
        private static void WriteShadows(ShadowQuality value) { if(value!=ShadowQuality.Disable) QualitySettings.shadows=value; }
        internal void ForgetSnapshot() { captured=false; }
    }
}
