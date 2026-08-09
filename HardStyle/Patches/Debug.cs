using UnityEngine;
using HarmonyLib;

namespace HardStyle.Patches;

[HarmonyPatch]
public class Debug
{
    // [HarmonyPostfix, HarmonyPatch(typeof(SpiderBody), nameof(SpiderBody.GetHurt))]
    // private static void MalfaceDebugger(ref SpiderBody __instance)
    // {
    //     if (__instance.eid != null)
    //     {
    //         Plugin.Logger.LogInfo($"SpiderBody.GetHurt() called, eid non-null, type of eid: {__instance.eid.GetType().Name}");
    //     }
    // }

    public static bool DEBUG_MODE = false;

    [HarmonyPrefix, HarmonyPatch(typeof(BloodsplatterManager), nameof(BloodsplatterManager.GetGore), 
                                 [ typeof(GoreType),
                                   typeof(bool),
                                   typeof(bool),
                                   typeof(bool),
                                   typeof(EnemyIdentifier),
                                   typeof(bool) ])]
    private static void GetGoreDebugger(ref EnemyIdentifier eid)
    {

        Log("BloodsplatterManager.GetGore() called.");
        if (eid != null)
        {
            Log($"EnemyIdentifier value non-null, value: {eid}");
        }
    }

    public static void Log(string text)
    {
        if (DEBUG_MODE)
        {
            Plugin.Logger.LogInfo(text);
        }
    }
}