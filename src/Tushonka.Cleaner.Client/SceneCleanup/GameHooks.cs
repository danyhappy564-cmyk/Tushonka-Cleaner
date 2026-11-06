// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using EFT.AssetsManager;

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
            Patch(typeof(MuzzleLight),"SetIntensity",nameof(MuzzleIntensity),null);
            Patch(typeof(AmmoPoolObject),"SetUsed",null,nameof(ShellUsed));
            Patch(typeof(AmmoPoolObject),"ReturnToPool",nameof(ShellReturned),null);
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
        private static void MuzzleIntensity(MuzzleLight __instance) { SceneCleanupModule.State?.OnMuzzle(__instance); }
        private static void ShellUsed(AmmoPoolObject __instance,bool isUsed) { SceneCleanupModule.State?.OnShell(__instance,isUsed); }
        private static void ShellReturned(AmmoPoolObject __instance) { SceneCleanupModule.State?.ForgetShell(__instance); }
    }
}
