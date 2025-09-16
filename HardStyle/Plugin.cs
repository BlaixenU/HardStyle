
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using HarmonyLib.Tools;


namespace HardStyle;

[HarmonyPatch]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; private set; } = null!;

    private void Awake()
    {

        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded! Yippee!!!");
        gameObject.hideFlags = HideFlags.DontSaveInEditor;

        DoPatching();

    }

    private static void DoPatching()
    {
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameStateManager), "CanSubmitScores", MethodType.Getter)]
    private static void ScoresSubmission(ref bool __result)
    {
        // prevent scores from being submitted since this mod is technically a cheat
        __result = false;
        // remove if using for a level plugin
        // thanks 10 days till xmas
    }
}

[HarmonyPatch]
public class HardDamagePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    private static void GetHurtPatch(ref float hardDamageMultiplier)
    {
        hardDamageMultiplier = 0.0f;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.ForceAntiHP))]
    private static bool ForceAntiHPPatch()
    {
        return false;
    }
}

[HarmonyPatch]
public class HealingPatches
{
    private static readonly float healStep = 0.2f / 6;
    private static readonly List<float> healMultipliers = [
        0.1f, // DESTRUCTIVE
        0.1f + healStep,
        0.1f + (2 * healStep),
        0.1f + (3 * healStep),
        0.1f + (4 * healStep),
        0.1f + (5 * healStep),
        0.3f,
        1f, // ULTRAKILL
    ];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHealth))]
    private static void GetHealthPatch(ref int health)
    {
        StyleHUD styleHud = StyleHUD.Instance;

        var currentHealMultiplier = healMultipliers[styleHud.rankIndex];
        var finalHealth = Mathf.RoundToInt(health * currentHealMultiplier);

        Plugin.Logger.LogInfo($"* GetHealth() called!");
        Plugin.Logger.LogInfo($"    Input {health}  output {finalHealth}");
        Plugin.Logger.LogInfo($"    Rank index {styleHud.rankIndex}  Multiplier {currentHealMultiplier}");
        Plugin.Logger.LogInfo("\n");

        health = finalHealth;
    }

    // probably wont understand it from the codematcher stuff but i effectively want the heal to follow this expression
    // (100 - NewMovement.hp) * healFactor
    // Since healFactor stuff is contained in NewMovement.GetHealth which already got patched,
    // all i have to do is insert IL to subtract hp from 100 and replace the existing argument of 999.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Parry))]
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

        codeMatcher.Start()
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
                   );

        return codeMatcher.InstructionEnumeration();
    }
}