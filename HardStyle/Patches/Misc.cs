using UnityEngine;
using HarmonyLib;
using ULTRAKILL.Cheats;

namespace HardStyle.Patches;

[HarmonyPatch]
public static class Misc
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

    [HarmonyPostfix, HarmonyPatch(typeof(LeaderboardController), nameof(LeaderboardController.CanSubmitScores), MethodType.Getter)]
    private static void ScoresSubmission(ref bool __result)
    {
        // prevent scores from being submitted since this mod is technically a cheat
        __result = false;
        // remove if using for a level plugin
        // thanks 10 days till xmas
    }

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