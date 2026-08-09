using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace HardStyle.Patches;

[HarmonyPatch]
public static class HardDamagePatches
{
    [HarmonyPrefix, HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    private static void GetHurtPatch(ref float hardDamageMultiplier)
    {
        hardDamageMultiplier = 0.0f;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NewMovement), nameof(NewMovement.ForceAntiHP))]
    private static bool ForceAntiHPPatch()
    {
        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.Start))]
    private static void HealthBarStartPatch(ref HealthBar __instance) // removes the hard damage slider thing 
    {
        Slider hdSlider;
        const string antiHpSliderName = "AntiHealth Slider";

        if (__instance.antiHpSlider != null)
        {
            hdSlider = __instance.antiHpSlider;
        }
        else if (__instance.gameObject.transform.Find(antiHpSliderName))
        {
            hdSlider = __instance.gameObject.transform.Find(antiHpSliderName).GetComponent<Slider>();
        }
        else
        {
            return;
        }

        hdSlider.gameObject.SetActive(false);
        __instance.antiHpSlider = null;
        

    }
}