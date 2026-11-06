// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using EFT.AssetsManager;
using GPUInstancer;

namespace Tushonka.Cleaner.SceneCleanup
{
    internal sealed class GameHooks
    {
        private readonly Harmony harmony=new Harmony("Tushonka.Cleaner.SceneCleanup.Cleanroom");
        internal void Install()
        {
            Type world=Type.GetType("EFT.GameWorld, Assembly-CSharp",true);
            Patch(world,"OnGameStarted",null,nameof(RaidStarted));
            Patch(world,"Dispose",nameof(RaidEnding),null);
            Patch(typeof(StaticDeferredDecalRenderer),"RegisterDecal",nameof(RegisterDecal),null);
            Patch(typeof(MuzzleLight),"OnShot",null,nameof(MuzzleShot));
            Patch(world,"SpawnShellInTheWorld",null,nameof(ShellSpawned));
            Patch(typeof(AmmoPoolObject),"ReturnToPool",nameof(ShellReturned),null);
            Patch(typeof(GPUInstancerTerrainManager),"get_isCulled",null,nameof(GrassCulled));
        }
        private void Patch(Type type,string name,string prefix,string postfix)
        {
            var method=AccessTools.DeclaredMethod(type,name);
            if(method==null) throw new MissingMethodException(type.FullName,name);
            harmony.Patch(method,prefix:prefix==null?null:new HarmonyMethod(typeof(GameHooks),prefix),postfix:postfix==null?null:new HarmonyMethod(typeof(GameHooks),postfix));
        }
        internal void Remove() { harmony.UnpatchSelf(); }
        private static void RaidStarted(object __instance) { SceneCleanupModule.State?.BeginRaid(__instance); }
        private static void RaidEnding(object __instance) { SceneCleanupModule.State?.EndRaid(__instance); }
        private static bool RegisterDecal(StaticDeferredDecal decal) { return SceneCleanupModule.State==null || !SceneCleanupModule.State.SkipDecal(decal); }
        private static void MuzzleShot(MuzzleLight __instance) { SceneCleanupModule.State?.OnMuzzle(__instance); }
        private static void ShellSpawned(AmmoPoolObject __result) { SceneCleanupModule.State?.OnShell(__result); }
        private static void ShellReturned(AmmoPoolObject __instance) { SceneCleanupModule.State?.ForgetShell(__instance); }
        // 게임은 지형을 그리지 않을 때 풀 매니저를 이 값으로 건너뜁니다. 매니저와 모션 벡터 정리는 그대로 돌고 그리기와 계산만 빠집니다.
        private static void GrassCulled(GPUInstancerTerrainManager __instance,ref bool __result)
        { if(!__result && __instance is GPUInstancerDetailManager && SceneCleanupModule.State!=null && SceneCleanupModule.State.GrassHidden) __result=true; }
    }
}
