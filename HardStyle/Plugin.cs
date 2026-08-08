
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using Unity;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine.AddressableAssets;
using ULTRAKILL.Cheats;




namespace HardStyle;

internal static class PluginInfo
{
    public const string PLUGIN_GUID = "com.blaixenu.hardstyle";
    public const string PLUGIN_NAME = "HardStyle";
    public const string PLUGIN_VERSION = "1.2.2";
}

[HarmonyPatch]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; private set; } = null!;

    private void Awake()
    {

        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} loaded! Yippee!!!");
        gameObject.hideFlags = HideFlags.DontSaveInEditor;

        DoPatching();

    }

    private static void DoPatching()
    {
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(LeaderboardController), nameof(LeaderboardController.CanSubmitScores), MethodType.Getter)]
    private static void ScoresSubmission(ref bool __result)
    {
        // prevent scores from being submitted since this mod is technically a cheat
        __result = false;
        // remove if using for a level plugin
        // thanks 10 days till xmas
    }
}

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
        if (!DEBUG_MODE) 
        {
            return;
        }

        Plugin.Logger.LogInfo("BloodsplatterManager.GetGore() called.");
        if (eid != null)
        {
            Plugin.Logger.LogInfo($"EnemyIdentifier value non-null, value: {eid}");
        }
    }
}

// PATCHES

[HarmonyPatch]
public class HardDamagePatches
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




[HarmonyPatch]
public class HealingPatches
{
    private static float healStep = 0.2f / 6;
    private static List<float> healMultipliers = [
                                                    0.1f, // DESTRUCTIVE
                                                    0.1f + healStep,
                                                    0.1f + (2 * healStep),
                                                    0.1f + (3 * healStep),
                                                    0.1f + (4 * healStep),
                                                    0.1f + (5 * healStep),
                                                    0.3f,
                                                    1f, // ULTRAKILL
                                                ];

    private static float HealFactor => healMultipliers[StyleHUD.Instance.rankIndex];
    
    [HarmonyPrefix, HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHealth))]
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
        }

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
    private static void BloodHealPrefix(ref Bloodsplatter __instance, ref int __state)
    {

        __state = __instance.hpAmount; // storing original hpAmount for the postfix

        UnityEngine.Object? obj = UnityEngine.Object.FindObjectFromInstanceID(__instance.eidID);

        if (Debug.DEBUG_MODE)
        {
            if (obj == null)
            {
                if (obj is null)
                {
                    Plugin.Logger.LogInfo("Bloodsplatter.eid is null.");
                }
                else
                {
                    Plugin.Logger.LogInfo("Bloodsplatter.eid is destroyed.");
                }
                return;
            }
            else
            {  
                Plugin.Logger.LogInfo($"Bloodsplatter.eid object name: {obj.name}");
            }
        }
        

        // HEALING CONDITIONS

        EnemyIdentifier? targetEid = obj as EnemyIdentifier; // ts cant be be null sybau

        switch (targetEid?.hitter)
        {
            case "drill":
                __instance.hpAmount = Mathf.RoundToInt(__instance.hpAmount * 0.5f);
                break;
            default:
                __instance.hpAmount = Mathf.RoundToInt(__instance.hpAmount * HealFactor);
                break;
        }
        
        if (Debug.DEBUG_MODE) 
        {
            Plugin.Logger.LogInfo($"Enemy hit, EnemyIdentifier found ({targetEid}), emit blood for {__instance.hpAmount} HP");
        }
    }
    
    
    [HarmonyPostfix, HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.Collide))]
    private static void BloodHealPostfix(ref Bloodsplatter __instance, ref int __state)
    {
        __instance.hpAmount = __state;
    }


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

[HarmonyPatch]
public class MiscPatches
{
    /* [HarmonyTranspiler, HarmonyPatch(typeof(SpiderBody), nameof(SpiderBody.GetHurt))]
    private static IEnumerable<CodeInstruction> MalfaceBloodsplatterInstanceFix(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
                   .MatchForward(true,
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(MonoSingleton<BloodsplatterManager>), "get_Instance"))
                   )
                   .ThrowIfInvalid("Could not find BloodsplatterManager.Instance() call.")
                   .MatchForward(true,
                    new CodeMatch(
                        OpCodes.Call, AccessTools.Method(
                            "UnityEngine.Object:Instantiate",
                            parameters: [typeof(GameObject), typeof(Vector3), typeof(Quaternion)],
                            generics: [typeof(GameObject)]))
                   )
                   .ThrowIfInvalid("Could not find UnityObject.Instantiate() call.")
                   .SetAndAdvance(OpCodes.Nop, null);

        return codeMatcher.InstructionEnumeration();
    } */

    [HarmonyPrefix, HarmonyPatch(typeof(SpiderBody), nameof(SpiderBody.GetHurt))]
    private static bool MalfaceFix(ref SpiderBody __instance,
                                   ref GameObject target,
                                   ref Vector3 force,
                                   ref Vector3 hitPoint,
                                   ref float multiplier,
                                   ref GameObject sourceWeapon)
    {
        var s = __instance;


        bool dead = false;
		float num = s.health;
		bool flag = true;
		if (hitPoint == Vector3.zero)
		{
			hitPoint = target.transform.position;
		}
		flag = MonoSingleton<BloodsplatterManager>.Instance.goreOn;
		if (s.eid == null)
		{
			s.eid = s.GetComponent<EnemyIdentifier>();
		}
		if (s.eid.hitter != "fire")
		{
			if (!s.eid.sandified && !s.eid.blessed)
			{
				GameObject gameObject = MonoSingleton<BloodsplatterManager>.Instance.GetGore(GoreType.Body, s.eid);
				if ((bool)gameObject)
				{
					Bloodsplatter component = gameObject.GetComponent<Bloodsplatter>();
					gameObject.transform.SetParent(s.gz.goreZone, worldPositionStays: true);
					if (s.eid.hitter == "drill")
					{
						gameObject.transform.localScale *= 2f;
					}
					if (s.health > 0f)
					{
						component.GetReady();
					}
					if (s.eid.hitter == "nail")
					{
						component.hpAmount = 3;
						component.GetComponent<AudioSource>().volume *= 0.8f;
					}
					else if (multiplier >= 1f)
					{
						component.hpAmount = 30;
					}
					if (flag)
					{
						gameObject.GetComponent<ParticleSystem>().Play();
					}
				}
				if (s.eid.hitter != "shotgun" && s.eid.hitter != "drill" && s.gameObject.activeInHierarchy)
				{
					if (s.dripBlood != null)
					{
						s.currentDrip = UnityEngine.Object.Instantiate(s.dripBlood, hitPoint, Quaternion.identity);
					}
					if ((bool)s.currentDrip)
					{
						s.currentDrip.transform.parent = s.transform;
						s.currentDrip.transform.LookAt(s.transform);
						s.currentDrip.transform.Rotate(180f, 180f, 180f);
						if (flag)
						{
							s.currentDrip.GetComponent<ParticleSystem>().Play();
						}
					}
				}
			}
			else
			{
				MonoSingleton<BloodsplatterManager>.Instance.GetGore(GoreType.Small, s.eid);
			}
		}
		if (!s.eid.dead)
		{
			if (!s.eid.blessed && !InvincibleEnemies.Enabled)
			{
				s.health -= 1f * multiplier;
			}
			if (s.scalc == null)
			{
				s.scalc = MonoSingleton<StyleCalculator>.Instance;
			}
			if (s.health <= 0f)
			{
				dead = true;
			}
			if (((s.eid.hitter == "shotgunzone" || s.eid.hitter == "hammerzone") && s.parryable) || s.eid.hitter == "punch")
			{
				if (s.parryable)
				{
					s.parryable = false;
					MonoSingleton<FistControl>.Instance.currentPunch.Parry(hook: false, s.eid);
					s.currentExplosion = UnityEngine.Object.Instantiate(s.beamExplosion.ToAsset(), s.transform.position, Quaternion.identity);
					if (!InvincibleEnemies.Enabled && !s.eid.blessed)
					{
						s.health -= (float)((s.parryFramesLeft > 0) ? 4 : 5) / s.eid.totalHealthModifier;
					}
					Explosion[] componentsInChildren = s.currentExplosion.GetComponentsInChildren<Explosion>();
					foreach (Explosion obj in componentsInChildren)
					{
						obj.speed *= s.eid.totalDamageModifier;
						obj.maxSize *= 1.75f * s.eid.totalDamageModifier;
						obj.damage = Mathf.RoundToInt(50f * s.eid.totalDamageModifier);
						obj.canHit = AffectedSubjects.EnemiesOnly;
						obj.friendlyFire = true;
					}
					if (s.currentEnrageEffect == null)
					{
						s.CancelInvoke("BeamFire");
						s.Invoke("StopWaiting", 1f);
						UnityEngine.Object.Destroy(s.currentChargeEffect);
					}
					s.parryFramesLeft = 0;
				}
				else
				{
					s.parryFramesLeft = MonoSingleton<FistControl>.Instance.currentPunch.activeFrames;
				}
			}
			if (multiplier != 0f)
			{
				s.scalc.HitCalculator(s.eid.hitter, "spider", "", dead, s.eid, sourceWeapon);
			}
			if (num >= s.maxHealth / 2f && s.health < s.maxHealth / 2f)
			{
				if (s.ensims == null || s.ensims.Length == 0)
				{
					s.ensims = s.GetComponentsInChildren<EnemySimplifier>();
				}
				UnityEngine.Object.Instantiate(s.woundedParticle, s.transform.position, Quaternion.identity);
				if (!s.eid.puppet)
				{
					EnemySimplifier[] array = s.ensims;
					foreach (EnemySimplifier enemySimplifier in array)
					{
						if (!enemySimplifier.ignoreCustomColor)
						{
							enemySimplifier.ChangeMaterialNew(EnemySimplifier.MaterialState.normal, s.woundedMaterial);
							enemySimplifier.ChangeMaterialNew(EnemySimplifier.MaterialState.enraged, s.woundedEnrageMaterial);
						}
					}
				}
			}
			if ((bool)s.hurtSound && num > 0f)
			{
				s.hurtSound.PlayClipAtPoint(MonoSingleton<AudioMixerController>.Instance.goreGroup, s.transform.position, 12, 1f, 0.75f, UnityEngine.Random.Range(0.85f, 1.35f));
			}
			if (s.health <= 0f && !s.eid.dead)
			{
				s.Die();
			}
		}
		else if (s.eid.hitter == "ground slam")
		{
			s.BreakCorpse();
		}

        return false;
    }
}

/* [HarmonyPatch]
public class ScrewdriverPatches
{

    [HarmonyTranspiler, HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.Collide))]
    private static IEnumerable<CodeInstruction> ThisTechniqueIsSoFunnyItMakesMeWannaMergeLanesWithoutLooking(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codeMatcher = new CodeMatcher(instructions, generator);

        // // MonoSingleton<NewMovement>.Instance.GetHealth(hpAmount, silent: false, fromExplosion);
        // IL_00c0: call !0 class MonoSingleton`1<class NewMovement>::get_Instance()
        // IL_00c5: ldarg.0
        // IL_00c6: ldfld int32 Bloodsplatter::hpAmount
        // IL_00cb: ldc.i4.0
        // IL_00cc: ldarg.0
        // IL_00cd: ldfld bool Bloodsplatter::fromExplosion
        // IL_00d2: ldc.i4.1
        // IL_00d3: callvirt instance void NewMovement::GetHealth(int32, bool, bool, bool)


        return codeMatcher.InstructionEnumeration();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.Collide))]
    private static void ConditionCollector(ref Bloodsplatter __instance, ref List<bool> __state, ref Collider other)
    {
        __state = new List<bool> { false, false, false };
        
        if (__instance.ready && !(__instance.bsm == null))
        {
            __state[0] = true;

            if (__instance.bsm.hasBloodFillers && ((__instance.bsm.bloodFillers.Contains(other.gameObject) && other.gameObject.TryGetComponent<BloodFiller>(out var component)) || ((bool)other.attachedRigidbody && __instance.bsm.bloodFillers.Contains(other.attachedRigidbody.gameObject) && other.attachedRigidbody.TryGetComponent<BloodFiller>(out component))))
            {
                __state[1] = true;
            }
            else if (__instance.canCollide && other.gameObject.CompareTag("Player"))
            {
                __state[2] = true;
            }
        } 
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Bloodsplatter), nameof(Bloodsplatter.Collide))]
    private static void Healer(ref Bloodsplatter __instance, ref List<bool> __state)
    {
        if (__state == new List<bool> { true, false, true })
        {
            
        }
    }
} */

