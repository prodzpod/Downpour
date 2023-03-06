using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Downpour
{
    public class Hooks
    {
        public static float lastStageTime = 0;
        public static int trueAmbientLevelFloor = -1;
        public static int previousAmbientLevelFloor = -1;
        public static bool onAmbientLevelUpEnabled = false;
        public static float targetTime = 0;
        public static void Patch()
        {
            Run.onRunStartGlobal += (run) => { if (DownpourPlugin.DEBUG) targetTime = DownpourPlugin.AutoAdvance[0].Value * 60f; lastStageTime = 0; trueAmbientLevelFloor = -1; previousAmbientLevelFloor = -1; Run.ambientLevelCap = int.MaxValue; };
            Stage.onStageStartGlobal += (stage) => { lastStageTime = Run.instance.GetRunStopwatch(); };
            if (Chainloader.PluginInfos.ContainsKey("com.Wolfo.LittleGameplayTweaks")) DownpourPlugin.Harmony.PatchAll(typeof(PatchWolfoSimu));
            if (Chainloader.PluginInfos.ContainsKey("BALLS.WellRoundedBalance")) DownpourPlugin.Harmony.PatchAll(typeof(PatchWRB));
            On.RoR2.LevelUpEffectManager.OnRunAmbientLevelUp += (orig, run) => { if (run.ambientLevelFloor > 100) return; orig(run); };
            On.RoR2.LevelUpEffectManager.OnCharacterLevelUp += (orig, body) => { if (body?.teamComponent?.teamIndex != null && body.teamComponent.teamIndex != TeamIndex.Player && Run.instance.ambientLevelFloor > 100) return; orig(body); };
        }
        public static void PatchRun(Run run)
        {
            if (DownpourPlugin.DEBUG) On.RoR2.Run.FixedUpdate += Run_FixedUpdate;
            if (!Enabled(run)) return;
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += Run_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
        }
        public static void UnpatchRun(Run _)
        {
            if (DownpourPlugin.DEBUG) On.RoR2.Run.FixedUpdate -= Run_FixedUpdate;
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal -= Run_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal -= InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
        }
        public static void Run_RecalculateDifficultyCoefficentInternal(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(x => x.MatchStfld<Run>(nameof(Run.compensatedDifficultyCoefficient)));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetCoeff);
            c.GotoNext(x => x.MatchStfld<Run>(nameof(Run.difficultyCoefficient)));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetCoeff);
            c.GotoNext(x => x.MatchCallOrCallvirt<Run>("set_" + nameof(Run.ambientLevel)));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetAmbient);
        }
        public static void InfiniteTowerRun_RecalculateDifficultyCoefficentInternal(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(x => x.MatchStfld<Run>(nameof(Run.difficultyCoefficient)));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetCoeff);
            c.GotoNext(x => x.MatchCallOrCallvirt<Run>("set_" + nameof(Run.ambientLevel)));
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Run, float>>(self => (self.compensatedDifficultyCoefficient - 1) * 3 + 1);
        }
        public static string[] targetStage = new string[] { "golemplains", "goolake", "frozenwall", "shipgraveyard", "slumberingsatellite" };
        public static void Run_FixedUpdate(On.RoR2.Run.orig_FixedUpdate orig, Run self)
        {
            orig(self);
            if (self.GetRunStopwatch() >= targetTime)
            {
                int idx = (self.stageClearCount + 1) % 5;
                targetTime += DownpourPlugin.AutoAdvance[idx].Value * 60f;
                RoR2.Console.instance.SubmitCmd(null, "next_stage " + targetStage[idx]);
            }
        }
        public static void PatchSimulacrum(Run run)
        {
            if (run is not InfiniteTowerRun) return;
            On.RoR2.InfiniteTowerWaveController.Initialize += InfiniteTowerWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.FixedUpdate += InfiniteTowerWaveController_FixedUpdate;
            CharacterBody.onBodyStartGlobal += CharacterBody_OnBodyStartGlobal;
        }
        public static void UnpatchSimulacrum(Run run)
        {
            if (run is not InfiniteTowerRun) return;
            On.RoR2.InfiniteTowerWaveController.Initialize -= InfiniteTowerWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.FixedUpdate -= InfiniteTowerWaveController_FixedUpdate;
            CharacterBody.onBodyStartGlobal -= CharacterBody_OnBodyStartGlobal;
        }
        public static void InfiniteTowerWaveController_Initialize(On.RoR2.InfiniteTowerWaveController.orig_Initialize orig, InfiniteTowerWaveController self, int index, Inventory inv, GameObject target)
        {
            if (DownpourPlugin.FasterSimulacrum.Value)
            {
                self.wavePeriodSeconds = 30f / Mathf.Clamp(GetScale(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty), Run.instance.GetRunStopwatch(), true), 1, 10);
                self.squadDefeatGracePeriod = 0;
                self.secondsBeforeSuddenDeath = 180f;
                self.secondsBeforeFailsafe = 10f;
            }
            self.secondsAfterWave = (int)(DownpourPlugin.DownpourList.Contains(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty)) ? DownpourPlugin.SimulacrumCountdownDownpour.Value : DownpourPlugin.SimulacrumCountdown.Value);
            orig(self, index, inv, target);
        }

        public static void InfiniteTowerWaveController_FixedUpdate(On.RoR2.InfiniteTowerWaveController.orig_FixedUpdate orig, InfiniteTowerWaveController self)
        {
            orig(self);
            if (DownpourPlugin.FasterSimulacrum.Value && self.combatSquad != null && self.combatSquad.memberCount == 0 && !self.haveAllEnemiesBeenDefeated)
            {
                self.totalWaveCredits -= self.creditsPerSecond;
                self.combatDirector.monsterCredit += self.creditsPerSecond;
                self.combatDirector.monsterSpawnTimer = 0;
            }
        }

        public static void CharacterBody_OnBodyStartGlobal(CharacterBody body)
        {
            if ((body.name.Contains("Brother") || body.name.Contains("TitanGold") || body.name.Contains("EquipmentDrone") || body.name.Contains("SuperRoboBallBoss")) && (Run.instance as InfiniteTowerRun).waveIndex < 50)
            {
                body.baseMaxHealth *= DownpourPlugin.SimulacrumBossHealth.Value;
                body.RecalculateStats();
                body.healthComponent.Networkhealth = body.maxHealth;
            }
        }

        [HarmonyPatch(typeof(DifficultyAPI), nameof(DifficultyAPI.InitialiseRuleBookAndFinalizeList))]
        public class PatchOrder
        {
            public static void Postfix(ref RuleDef __result)
            {
                Token.descs.Clear();
                foreach (var choice in __result.choices) 
                {
                    DifficultyDef def = DifficultyCatalog.GetDifficultyDef(choice.difficultyIndex);
                    Token.descs.Add(def.descriptionToken, def);
                }
                if (DownpourPlugin.EnableDownpour.Value || DownpourPlugin.EnableBrimstone.Value) __result.choices.Sort((x, y) => 
                GetSortValue(DifficultyCatalog.GetDifficultyDef(x.difficultyIndex))
                .CompareTo(GetSortValue(DifficultyCatalog.GetDifficultyDef(y.difficultyIndex))));
            }

            public static float GetSortValue(DifficultyDef def)
            {
                return def.scalingValue
                    + (5.9f * (def.nameToken == "INFERNO_NAME" ? 1 : 0))
                    + (1000 * (DownpourPlugin.DownpourList.Contains(def) ? 1 : 0))
                    + (1000 * (DownpourPlugin.BrimstoneList.Contains(def) ? 1 : 0));
            }
        }

        [HarmonyPatch]
        public class InfernoButCringePatch
        {
            public static void ILManipulator(ILContext il)
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchLdarg(1));
                c.Index++;
                while (c.Next.OpCode != OpCodes.Stloc_0) c.Remove();
                c.EmitDelegate<Func<Run, bool>>(run => { return run.selectedDifficulty == Inferno.Main.InfernoDiffIndex || DownpourPlugin.BrimstoneList.Contains(DifficultyCatalog.GetDifficultyDef(run.selectedDifficulty)); });
            }

            public static MethodBase TargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(Inferno.Main).GetNestedType("<>c", AccessTools.all), "<Awake>b__158_0");
            }
        }

        [HarmonyPatch(typeof(Inferno.Unlocks.BasePerSurvivorClearGameInfernoAchievement), nameof(Inferno.Unlocks.BasePerSurvivorClearGameInfernoAchievement.OnClientGameOverGlobal))]
        public class BrimstoneAchievementPatch
        {
            public static void ILManipulator(ILContext il)
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchLdloc(2));
                c.Index++;
                while (c.Next.OpCode != OpCodes.Stloc_3) c.Remove();
                c.EmitDelegate<Func<DifficultyDef, bool>>(diff =>
                {
                    return diff != null && (diff == Inferno.Main.InfernoDiffDef || DownpourPlugin.BrimstoneList.Contains(diff));
                });
            }
        }

        [HarmonyPatch]
        public class PatchWolfoSimu
        {
            public static bool Prefix(ref On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, ref InfiniteTowerRun self)
            {
                if (Enabled(self)) { orig(self); return false; }
                return true;
            }

            // bless you aaron
            public static MethodBase TargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(LittleGameplayTweaks.LittleGameplayTweaks).GetNestedType("<>c", AccessTools.all), $"<{nameof(LittleGameplayTweaks.LittleGameplayTweaks.SimuChanges)}>b__273_2");
            }
        }

        [HarmonyPatch]
        public class PatchWRB
        {
            public static bool Prefix(ref On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, ref InfiniteTowerRun self)
            {
                if (DownpourPlugin.DownpourList.Contains(DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty))) { orig(self); return false; } // still apply downpour scaling for downpour
                return true; // wrb scaling for rest
            }

            public static MethodBase TargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(WellRoundedBalance.Mechanics.Scaling.TimeScaling).GetNestedType("<>c", AccessTools.all), $"<{nameof(WellRoundedBalance.Mechanics.Scaling.TimeScaling.ChangeBehavior)}>b__10_0");
            }
        }

        public static bool Enabled(Run run) { return Enabled(DifficultyCatalog.GetDifficultyDef(run.selectedDifficulty), run is InfiniteTowerRun); }
        public static bool Enabled(DifficultyDef def, bool isSimulacrum)
        {
            if (!DownpourPlugin.DownpourList.Contains(def)) // enables
            {
                if (def.nameToken == "SUNNY_NAME") return false;
                if (Chainloader.PluginInfos.ContainsKey("BALLS.WellRoundedBalance") && !isSimulacrum && WRBTweaksOn()) return false;
                if (def.nameToken == "INFERNO_NAME") { if (!DownpourPlugin.EnableInferno.Value) return false; }
                else { if (!DownpourPlugin.EnableRework.Value) return false; }
            }
            DownpourPlugin.Log.LogDebug("Downpour is enabled for " + Language.GetString(def.nameToken));
            return true;
        }
        public static bool WRBTweaksOn() 
        {
            return (Chainloader.PluginInfos[WellRoundedBalance.Main.PluginGUID].Instance as WellRoundedBalance.Main).ValidateMechanic(WellRoundedBalance.Mechanics.Scaling.TimeScaling.instance); // based
        }

        public static float GetCoeff(float _, Run self)
        {
            float sec = self.GetRunStopwatch();
            float min = Mathf.Floor(sec * 0.01666667f);
            DifficultyDef def = DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty);
            float people = (0.3f * self.participatingPlayerCount) + 0.7f;
            float stage = GetStageScale(def, self is InfiniteTowerRun ? (self as InfiniteTowerRun).waveIndex / 5 : self.stageClearCount, self.participatingPlayerCount, self is InfiniteTowerRun);
            float scale = GetScale(def, sec, self is InfiniteTowerRun);
            return Mathf.Clamp((stage * min + people) * scale, 0, int.MaxValue / 3 - 2);
        }

        public static float GetAmbient(float _, Run self)
        {
            float people = (0.3f * self.participatingPlayerCount) + 0.7f;
            return (self.difficultyCoefficient - people) * 3 + 1; // no cap fr fr
        }

        public static float GetScale(DifficultyDef def, float sec, bool simulacrum = false)
        {
            if (def == null) return 1;
            float diff; float temp;
            if (DownpourPlugin.BrimstoneList.Contains(def)) // brimstone
            {
                diff = DownpourPlugin.ScalingBrimstone.Value;
                if (simulacrum) diff = (diff + DownpourPlugin.SimulacrumBaseBrimstone.Value) * DownpourPlugin.SimulacrumScalingBrimstone.Value;
                temp = diff * DownpourPlugin.TempScalingBrimstone.Value;
                if (simulacrum) temp *= DownpourPlugin.SimulacrumTempScalingBrimstone.Value;
            }
            else if (DownpourPlugin.DownpourList.Contains(def)) // downpour
            {
                diff = DownpourPlugin.ScalingDownpour.Value;
                if (simulacrum) diff = (diff + DownpourPlugin.SimulacrumBaseDownpour.Value) * DownpourPlugin.SimulacrumScalingDownpour.Value;
                temp = diff * DownpourPlugin.TempScalingDownpour.Value;
                if (simulacrum) temp *= DownpourPlugin.SimulacrumTempScalingDownpour.Value;
            }
            else if (def.nameToken == "INFERNO_NAME") // inferno
            {
                diff = DownpourPlugin.ScalingInferno.Value;
                if (simulacrum) diff = (diff + DownpourPlugin.SimulacrumBase.Value) * DownpourPlugin.SimulacrumScaling.Value;
                temp = diff * DownpourPlugin.TempScaling.Value;
                if (simulacrum) temp *= DownpourPlugin.SimulacrumTempScaling.Value;
            }
            else // normal
            {
                float mult = (DownpourPlugin.ScalingDrizzle.Value - DownpourPlugin.ScalingMonsoon.Value) / 2;
                float init = DownpourPlugin.ScalingDrizzle.Value + mult;
                diff = Mathf.Max(DownpourPlugin.ScalingMax.Value, init - (mult * def.scalingValue));
                if (simulacrum) diff = (diff + DownpourPlugin.SimulacrumBase.Value) * DownpourPlugin.SimulacrumScaling.Value;
                temp = diff * DownpourPlugin.TempScaling.Value;
                if (simulacrum) temp *= DownpourPlugin.SimulacrumTempScaling.Value;
            }
            return GetScaleInternal(diff, temp, sec);
        }
        public static float GetScaleInternal(float diff, float temp, float sec) { return Mathf.Pow(1.15f, (diff == 0 ? 0 : (sec / diff)) + (temp == 0 ? 0 : ((sec - lastStageTime) / temp))); }

        public static float GetStageScale(DifficultyDef def, int stage, int people, bool simulacrum = false)
        {
            if (def == null) return 1;
            if (DownpourPlugin.BrimstoneList.Contains(def)) return GetStageScaleInternal(DownpourPlugin.InitialScalingBrimstone.Value + (stage * DownpourPlugin.StageScalingBrimstone.Value), people) * (simulacrum ? DownpourPlugin.SimulacrumStageScalingBrimstone.Value : 1); // brimstone
            if (DownpourPlugin.DownpourList.Contains(def)) return GetStageScaleInternal(DownpourPlugin.InitialScalingDownpour.Value + (stage * DownpourPlugin.StageScalingDownpour.Value), people) * (simulacrum ? DownpourPlugin.SimulacrumStageScalingDownpour.Value : 1); // downpour
            return GetStageScaleInternal(def.scalingValue + DownpourPlugin.InitialScaling.Value + (stage * DownpourPlugin.StageScaling.Value), people) * (simulacrum ? DownpourPlugin.SimulacrumStageScaling.Value : 1); // normal, inferno
        }
        public static float GetStageScaleInternal(float coeff, int people) { return 0.0506f * coeff * Mathf.Pow(people, 0.2f); }

        public static void PatchGup()
        {
            foreach (var rawBody in new string[] { "Gup", "Geep", "Gip" })
            {
                CharacterBody body = Addressables.LoadAssetAsync<GameObject>($"RoR2/DLC1/Gup/{rawBody}Body.prefab").WaitForCompletion().GetComponent<CharacterBody>();
                body.baseMaxHealth *= 0.75f;
                body.levelMaxHealth *= 0.75f;
                body.baseRegen = 0;
                body.levelRegen = 0;
                if (rawBody != "Gup")
                {
                    body.baseDamage *= 4f / 3f;
                    body.levelDamage *= 4f / 3f;
                    body.baseMoveSpeed *= 1.5f; // zoom
                    body.levelMoveSpeed *= 1.5f;
                }
            }
            IL.RoR2.MasterSummon.Perform += (il) =>
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<TeamComponent>(nameof(TeamComponent.GetTeamMembers)));
                c.Emit(OpCodes.Dup);
                c.Index++;
                c.EmitDelegate<Func<ReadOnlyCollection<TeamComponent>, int, int>>((list, orig) =>
                {
                    foreach (TeamComponent team in list) if (team.body != null && team.body.name.Contains("GupBody")) orig += 2;
                    return orig;
                });
            };
        }
    }
}
