using BepInEx.Bootstrap;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Reflection;
using UnityEngine;

namespace Downpour
{
    public class Hooks
    {
        public static float lastStageTime = 0;
        public static int trueAmbientLevelFloor = -1;
        public static int previousAmbientLevelFloor = -1;
        public static bool onAmbientLevelUpEnabled = false;
        public static void Patch()
        {
            Run.onRunStartGlobal += (run) => { lastStageTime = 0; trueAmbientLevelFloor = -1; previousAmbientLevelFloor = -1; };
            Stage.onStageStartGlobal += (stage) => { lastStageTime = Run.instance.GetRunStopwatch(); };
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += (il) => // hopefully noone does il hooks on this smile
            {
                ILCursor c = new(il);
                ILLabel label = null;
                c.GotoNext(x => x.MatchRet()); c.GotoPrev(x => x.MatchBeq(out label));
                c.Index = 0;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Run, bool>>(self => { return BasedRecalculateDifficultyInternal(self); });
                c.Emit(OpCodes.Ldc_I4_0).Emit(OpCodes.Beq, label);
            };
            IL.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += (il) =>
            {
                ILCursor c = new(il);
                ILLabel label = null;
                c.GotoNext(x => x.MatchRet()); c.GotoPrev(x => x.MatchBeq(out label));
                c.Index = 0;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<InfiniteTowerRun, bool>>(self => { return BasedRecalculateDifficultyInternal(self); });
                c.Emit(OpCodes.Ldc_I4_0).Emit(OpCodes.Beq, label);
            };
            if (Chainloader.PluginInfos.ContainsKey("com.Wolfo.LittleGameplayTweaks")) DownpourPlugin.Harmony.PatchAll(typeof(PatchWolfoSimu));
        }

        public static void PatchSimulacrum()
        {
            On.RoR2.InfiniteTowerWaveController.Initialize += (orig, self, index, inv, target) =>
            {
                self.wavePeriodSeconds = 30f / Mathf.Min(10, GetScale(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty), Run.instance.GetRunStopwatch(), true));
                self.squadDefeatGracePeriod = 0;
                self.secondsBeforeSuddenDeath = 180f;
                self.secondsBeforeFailsafe = 10f;
                orig(self, index, inv, target);
            };
            On.RoR2.InfiniteTowerWaveController.FixedUpdate += (orig, self) =>
            {
                orig(self);
                if (self.combatSquad != null && self.combatSquad.memberCount == 0 && !self.haveAllEnemiesBeenDefeated)
                {
                    self.totalWaveCredits -= self.creditsPerSecond;
                    self.combatDirector.monsterCredit += self.creditsPerSecond;
                    self.combatDirector.monsterSpawnTimer = 0;
                }
            };
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
                {
                    DifficultyDef xDef = DifficultyCatalog.GetDifficultyDef(x.difficultyIndex);
                    DifficultyDef yDef = DifficultyCatalog.GetDifficultyDef(y.difficultyIndex);
                    float xValue = xDef.scalingValue + (1000 * (DownpourPlugin.DownpourList.Contains(xDef) ? 1 : 0)) + (1000 * (DownpourPlugin.BrimstoneList.Contains(xDef) ? 1 : 0));
                    float yValue = yDef.scalingValue + (1000 * (DownpourPlugin.DownpourList.Contains(yDef) ? 1 : 0)) + (1000 * (DownpourPlugin.BrimstoneList.Contains(yDef) ? 1 : 0));
                    return xValue.CompareTo(yValue);
                });
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
                return BasedRecalculateDifficultyInternal(self);
            }

            // bless you aaron
            public static MethodBase TargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(LittleGameplayTweaks.LittleGameplayTweaks).GetNestedType("<>c", AccessTools.all), "<SimuChanges>b__273_2");
            }
        }

        public static bool BasedRecalculateDifficultyInternal(Run self)
        {
            if (!DownpourPlugin.DownpourList.Contains(DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty))) // enables
            {
                if (DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty).nameToken == "INFERNO_NAME") { if (!DownpourPlugin.EnableInferno.Value) return true; }
                else { if (!DownpourPlugin.EnableRework.Value) return true; }
            }
            float sec = self.GetRunStopwatch();
            float min = Mathf.Floor(sec * 0.01666667f);
            DifficultyDef def = DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty);
            float people = (0.3f * self.participatingPlayerCount) + 0.7f;
            float stage = GetStageScale(def, self is InfiniteTowerRun ? (self as InfiniteTowerRun).waveIndex / 5 : self.stageClearCount, self.participatingPlayerCount);
            float scale = GetScale(def, sec);
            self.difficultyCoefficient = (stage * min + people) * scale;
            self.compensatedDifficultyCoefficient = self.difficultyCoefficient;
            self.ambientLevel = (self.difficultyCoefficient - people) / 0.33f + 1; // no cap fr fr
            self.ambientLevelFloor = Mathf.FloorToInt(self.ambientLevel);
            trueAmbientLevelFloor = self.ambientLevelFloor;
            if (previousAmbientLevelFloor < self.ambientLevelFloor)
            {
                previousAmbientLevelFloor = self.ambientLevelFloor;
                if (self.ambientLevelFloor > 1) self.OnAmbientLevelUp();
            }
            return false;
        }

        public static float GetScale(DifficultyDef def, float sec, bool simulacrum = false)
        {
            if (def == null) return 1;
            float diff; float temp = 0;
            if (DownpourPlugin.BrimstoneList.Contains(def)) // brimstone
            {
                diff = DownpourPlugin.ScalingBrimstone.Value;
                if (simulacrum) diff = (diff + DownpourPlugin.SimulacrumBase.Value) * DownpourPlugin.SimulacrumTempScalingBrimstone.Value;
                temp = diff * DownpourPlugin.TempScalingBrimstone.Value;
                if (simulacrum) temp *= DownpourPlugin.SimulacrumTempScalingBrimstone.Value;
            }
            else if (DownpourPlugin.DownpourList.Contains(def)) // downpour
            {
                diff = DownpourPlugin.ScalingDownpour.Value;
                if (simulacrum) diff = (diff + DownpourPlugin.SimulacrumBase.Value) * DownpourPlugin.SimulacrumTempScalingDownpour.Value;
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
        public static float GetScaleInternal(float diff, float temp, float sec) { return Mathf.Pow(1.15f, (diff <= 0 ? 0 : (sec / diff)) + (temp <= 0 ? 0 : ((sec - lastStageTime) / temp))); }

        public static float GetStageScale(DifficultyDef def, int stage, int people)
        {
            if (def == null) return 1;
            if (DownpourPlugin.BrimstoneList.Contains(def)) return GetStageScaleInternal(DownpourPlugin.InitialScalingBrimstone.Value + (stage * DownpourPlugin.StageScalingBrimstone.Value), people); // brimstone
            if (DownpourPlugin.DownpourList.Contains(def)) return GetStageScaleInternal(DownpourPlugin.InitialScalingDownpour.Value + (stage * DownpourPlugin.StageScalingDownpour.Value), people); // downpour
            return GetStageScaleInternal(def.scalingValue + DownpourPlugin.InitialScaling.Value + (stage * DownpourPlugin.StageScaling.Value), people); // normal, inferno
        }
        public static float GetStageScaleInternal(float coeff, int people) { return 0.0506f * coeff * Mathf.Pow(people, 0.2f); }
    }
}
