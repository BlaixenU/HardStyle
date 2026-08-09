using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;

namespace HardStyle.Patches;

[HarmonyPatch]
public static class Healing
{
    private static float HealFactor => Config.healMults[StyleHUD.Instance.rankIndex].value;

    private static bool screwdriverBypass => Config.screwBypass.value;

    private static float screwdriverMultiplier => Config.screwMult.value;

    private static bool roundingFix => Config.roundingFix.value;
    
    /* [HarmonyPrefix, HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHealth))] dont uncomment this brah.
    private static void GetHealthPatch(ref int health)
    {
        StyleHUD styleHud = StyleHUD.Instance;
        var currentHealMultiplier = healMultipliers[styleHud.rankIndex];
        var finalHealth = Mathf.RoundToInt(health * currentHealMultiplier);
        if (Debug.DEBUG_MODE)
        {
            Plugin.Logger.LogInfo($"* GetHealth() called!");
            Plugin.Logger.LogInfo($"    Input {health}  output {finalHealth}");
            Plugin.Logger.LogInfo($"    Rank index {styleHud.rankIndex}  Multiplier {currentHealMultiplier}");
            Plugin.Logger.LogInfo("\n");
            return;
        }
        health = finalHealth;
    } */

    /* [HarmonyPrefix, HarmonyPatch(typeof(BloodsplatterManager), nameof(BloodsplatterManager.PrepareGore))]
    private static bool ThisShouldBeATranspilerButIsnt(ref GameObject gob, ref int healthChange, ref EnemyIdentifier eid, ref bool fromExplosion)
    {
        if ((healthChange >= 0 || (eid != null) || fromExplosion) && gob.TryGetComponent<Bloodsplatter>(out var component))
        {
            if ((bool)eid)
            {
                component.eid = eid;
            }
            if (healthChange >= 0)
            {

                if (eid == null)
                {
                    component.hpAmount = (int)(healthChange * GetHealFactor());
                }
                else
                {
                    Plugin.Logger.LogInfo("eid found");
                    switch (eid.hitter)
                    {
                        case "drill":
                            component.hpAmount = healthChange;
                            Plugin.Logger.LogInfo("Hitter type: drill");
                            Plugin.Logger.LogInfo($"Set hpAmount for: {component.hpAmount}");
                            break;
                        default:
                            component.hpAmount = (int)(healthChange * GetHealFactor());
                            Plugin.Logger.LogInfo("Default hitter type");
                            Plugin.Logger.LogInfo($"Set hpAmount for: {component.hpAmount}");
                            break;
                    }
                }

            }
            if (fromExplosion)
            {
                component.fromExplosion = true;
            }
        }

        return false;
    } */

    [HarmonyPrefix, HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.Collide))]
    private static void BloodHealPrefix(ref Bloodsplatter __instance)
    {

        var originalHpAmount = __instance.hpAmount; // storing original hpAmount for the postfix

        UnityEngine.Object? obj = UnityEngine.Object.FindObjectFromInstanceID(__instance.eidID);

        if (obj == null)
        {
            if (obj is null)
            {
                Debug.Log("Bloodsplatter.eid is null.");
            }
            else
            {
                Debug.Log("Bloodsplatter.eid is destroyed.");
            }
            return;
        }
        else
        {  
            Debug.Log($"Bloodsplatter.eid object name: {obj.name}");
        }
        
        

        // HEALING CONDITIONS

        EnemyIdentifier targetEid = null!;
        targetEid = (EnemyIdentifier)obj;

        /* switch (targetEid?.hitter)
        {
            case "drill":
                __instance.hpAmount = Mathf.RoundToInt(__instance.hpAmount * 0.5f);
                break;
            default:
                __instance.hpAmount = Mathf.RoundToInt(__instance.hpAmount * HealFactor);
                break;
        } */
        
        __instance.hpAmount = Mathf.RoundToInt(originalHpAmount * HealFactor);
        if (targetEid?.hitter == "drill")
        {
            __instance.hpAmount = Mathf.RoundToInt(originalHpAmount * screwdriverMultiplier);
        }


        Debug.Log($"Enemy hit, EnemyIdentifier found ({targetEid}), emit blood for {__instance.hpAmount} HP");

    }
    
    
    /* [HarmonyPostfix, HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.Collide))]
    private static void BloodHealPostfix(ref Bloodsplatter __instance, ref int __state)
    {
        __instance.hpAmount = __state; // what does this even do?
    }
 */

    [HarmonyTranspiler, HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Parry))]
    private static IEnumerable<CodeInstruction> ParryPatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codeMatcher = new CodeMatcher(instructions, generator);

        /* IL_0000: call !0 class MonoSingleton`1<class TimeController>::get_Instance()
		IL_0005: callvirt instance void TimeController::ParryFlash()
		IL_000a: ldarg.0        <- push 'this'
		IL_000b: ldc.i4.0       <- push 'false'
		IL_000c: stfld bool NewMovement::exploded    <- override value of 'exploded' to 'false'
		IL_0011: ldarg.0        <- push 'this'
		IL_0012: ldc.i4 999     <- push 999
		IL_0017: ldc.i4.0       <- push false
		IL_0018: ldc.i4.0       <- push false
		IL_0019: ldc.i4.1       <- push true
		IL_001a: call instance void NewMovement::GetHealth(int32, bool, bool, bool) */

        /* IL_0000: call !0 class MonoSingleton`1<class TimeController>::get_Instance()
		IL_0005: callvirt instance void TimeController::ParryFlash()
		IL_000a: ldarg.0
		IL_000b: ldc.i4.0
		IL_000c: stfld bool NewMovement::exploded
		IL_0011: ldarg.0
                 ldarg.0
                 ldfld int NewMovement::hp
                 ldc.i4 100
		IL_0012: sub
		IL_0017: ldc.i4.0
		IL_0018: ldc.i4.0
		IL_0019: ldc.i4.1
		IL_001a: call instance void NewMovement::GetHealth(int32, bool, bool, bool) */

        /* codeMatcher.Start()
                   .MatchForward(true,
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(NewMovement), nameof(NewMovement.exploded))),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldc_I4, 999)
                   )
                   .Set(OpCodes.Nop, null)
                   .Insert(
                    new CodeInstruction(OpCodes.Ldc_I4, 100),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(NewMovement), nameof(NewMovement.hp))),
                    new CodeInstruction(OpCodes.Sub)
                   ); */

        codeMatcher.Start()
                   .MatchForward(true,
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(NewMovement), nameof(NewMovement.exploded))),
                    new CodeMatch(OpCodes.Ldarg_0)
                   )
                   .SetAndAdvance(OpCodes.Nop, null)
                   .SetAndAdvance(OpCodes.Nop, null)
                   .SetAndAdvance(OpCodes.Nop, null)
                   .SetAndAdvance(OpCodes.Nop, null)
                   .SetAndAdvance(OpCodes.Nop, null)
                   .Set(OpCodes.Nop, null);

        return codeMatcher.InstructionEnumeration();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Parry))]
    private static void ParryPostfix(ref NewMovement __instance)
    {
        float healAmount = (100 - __instance.hp) * HealFactor;

        __instance.GetHealth(Mathf.RoundToInt(healAmount), false);
    }
    
}