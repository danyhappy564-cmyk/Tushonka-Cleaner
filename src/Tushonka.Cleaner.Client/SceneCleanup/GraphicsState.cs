// SPDX-License-Identifier: GPL-3.0-only
using UnityEngine;

namespace Tushonka.Cleaner.SceneCleanup
{
    internal sealed class GraphicsState
    {
        private bool captured;
        private int particleBudget,pixelLights,cascades;
        private bool softParticles,softVegetation,reflections;
        private ShadowQuality shadows;
        private ShadowResolution resolution;
        private float lodBias,shadowDistance;
        internal void Capture()
        {
            if(captured) return;
            particleBudget=QualitySettings.particleRaycastBudget; pixelLights=QualitySettings.pixelLightCount;
            softParticles=QualitySettings.softParticles; softVegetation=QualitySettings.softVegetation;
            reflections=QualitySettings.realtimeReflectionProbes; shadows=QualitySettings.shadows;
            resolution=QualitySettings.shadowResolution; cascades=QualitySettings.shadowCascades;
            lodBias=QualitySettings.lodBias; shadowDistance=QualitySettings.shadowDistance; captured=true;
        }
        // 한 Tick에 한 속성만 적용하여 설정 변경을 분산합니다.
        internal void Apply(int slot,CleanupSettings s,SceneCleanupLevel level)
        {
            if(!captured) return;
            bool active=level>=SceneCleanupLevel.Balanced; bool max=level==SceneCleanupLevel.Max;
            switch(slot)
            {
                case 0: QualitySettings.particleRaycastBudget=active&&s.Particles.Value ? Mathf.Min(particleBudget,max?32:128):particleBudget; break;
                case 1: QualitySettings.softParticles=active&&s.SoftParticles.Value?false:softParticles; break;
                case 2: QualitySettings.softVegetation=active&&s.SoftVegetation.Value?false:softVegetation; break;
                case 3: QualitySettings.realtimeReflectionProbes=active&&s.Reflections.Value?false:reflections; break;
                case 4: QualitySettings.shadows=active&&s.Shadows.Value?(max?ShadowQuality.Disable:(shadows==ShadowQuality.All?ShadowQuality.HardOnly:shadows)):shadows; break;
                case 5: QualitySettings.shadowResolution=active&&s.Shadows.Value?(ShadowResolution)Mathf.Min((int)resolution,max?0:1):resolution; break;
                case 6: QualitySettings.shadowCascades=active&&s.Shadows.Value?Mathf.Min(cascades,max?0:2):cascades; break;
                case 7: QualitySettings.shadowDistance=active&&s.Shadows.Value?Mathf.Min(shadowDistance,max?40:80):shadowDistance; break;
                case 8: QualitySettings.pixelLightCount=active&&s.Lights.Value?Mathf.Min(pixelLights,max?1:2):pixelLights; break;
                case 9: QualitySettings.lodBias=active&&s.Lod.Value?Mathf.Min(lodBias,max?.5f:.8f):lodBias; break;
            }
        }
        internal void Restore()
        {
            if(!captured) return;
            QualitySettings.particleRaycastBudget=particleBudget; QualitySettings.pixelLightCount=pixelLights;
            QualitySettings.softParticles=softParticles; QualitySettings.softVegetation=softVegetation;
            QualitySettings.realtimeReflectionProbes=reflections; QualitySettings.shadows=shadows;
            QualitySettings.shadowResolution=resolution; QualitySettings.shadowCascades=cascades;
            QualitySettings.lodBias=lodBias; QualitySettings.shadowDistance=shadowDistance; captured=false;
        }
        internal void ForgetSnapshot() { captured=false; }
    }
}
