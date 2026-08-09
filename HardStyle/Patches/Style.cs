using UnityEngine;
using HarmonyLib;

namespace HardStyle.Patches;

[HarmonyPatch]
public static class Style
{
    public static float timeSinceHook;

    public static float originalDecaySpeed;

    public static bool doWhiplashStyleReduction => Config.whipStyleDecayAccel.value;

    public static bool doWhiplashStyleReductionEase => Config.rateIncreaseEase.value;

    public static float baseDecayMultiplier => Config.startingDecayRate.value;

    public static float finalDecayMultiplier => Config.finalDecayRate.value;

    public static float hookTime => Config.easeTime.value;

    public static bool IsBeingPulledToEnemy
    {
        get
        {
            var whiplash = HookArm.Instance;

            if (whiplash.state == HookState.Pulling && (bool)whiplash.caughtEid && !whiplash.lightTarget)
            {
                return true;
            }
            return false;
        }
    }

    public static float RateMultiplier
    {
        get
        {
            float result = baseDecayMultiplier;

            if (doWhiplashStyleReductionEase)
            {
                result += Mathf.Clamp01(timeSinceHook / hookTime) * (finalDecayMultiplier - baseDecayMultiplier);
            }

            return result;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HookArm), nameof(HookArm.Update))]
    public static void Counter()
    {
        if (IsBeingPulledToEnemy)
        {
            timeSinceHook += Time.deltaTime * Time.timeScale;
        }
        else
        {
            timeSinceHook = 0;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(StyleHUD), nameof(StyleHUD.UpdateMeter))]
    public static void skababa1(ref float __state, ref StyleHUD __instance)
    {
        __state = __instance.currentMeter;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(StyleHUD), nameof(StyleHUD.UpdateMeter))]
    public static void skababa2(ref float __state, ref StyleHUD __instance)
    {
        if (!doWhiplashStyleReduction)
        {
            return;
        }

        __instance.currentMeter = __state;
        
        if (!(__instance.currentMeter > 0f && !__instance.comboActive) && !(__instance.currentMeter < 0f))
        {
            float factor = 1.0f;

            if (IsBeingPulledToEnemy)
            {
                factor = RateMultiplier;
            }

            __instance.currentMeter -= Time.deltaTime * (__instance.currentRank.drainSpeed * factor * 15f);
        }
    }


    /* [HarmonyPrefix, HarmonyPatch(typeof(StyleHUD), nameof(StyleHUD.AddFreshness))]
    public static void WhiplashStyleDrainer(ref float amt)
    {
        if (amt >= 0)
        {
            return;
        }
        if (!HookArm.Instance || !HookArm.Instance.equipped)
        {
            return;
        }
        
        if (IsBeingPulledToEnemy)
        {
            var factor = baseDecayMultiplier + (Mathf.Clamp01(timeSinceHook / hookTime) * (finalDecayMultiplier - baseDecayMultiplier));

            amt *= factor;
        }
    } */

}