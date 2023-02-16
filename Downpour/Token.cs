using HarmonyLib;
using RoR2;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Downpour
{
    public class Token
    {
        public static Dictionary<string, DifficultyDef> descs = new();
        public static void Patch()
        {
            On.RoR2.Language.GetLocalizedStringByToken += (orig, self, token) =>
            {
                if (descs.ContainsKey(token)) return GetDescription(orig(self, token), descs[token]);
                return orig(self, token);
            };
        }

        public static void PatchBrimstone()
        {
            On.RoR2.Language.GetLocalizedStringByToken += (orig, self, token) =>
            {
                if (token == "BRIMSTONE_DESC")
                {
                    string str = "<style=cStack>>Player Health Regeneration: <style=cIsHealth>-40%</style>\n";
                    if (Inferno.Main.Scaling.Value > 0f && Inferno.Main.Scaling.Value != 100f) str += $">Difficulty Scaling: <style={(Inferno.Main.Scaling.Value < 100f ? "cIsHealing" : "cIsHealth")}>{PercentFormat((Inferno.Main.Scaling.Value - 100f) / 100f)}% + Endless</style>\n";
                    if (Inferno.Main.LevelAttackSpeed.Value > 0f || Inferno.Main.LevelMoveSpeed.Value > 0f || Inferno.Main.LevelRegen.Value > 0) str += ">Enemy Stats: <style=cIsHealth>Constantly Increasing</style>\n";
                    if (Inferno.Main.ProjectileSpeed.Value > 1f) str += ">Enemy Projectile Speed: <style=cIsHealth>+" + ((Inferno.Main.ProjectileSpeed.Value - 1f) * 100f) + "%</style>\n";
                    if (Inferno.Main.EnableCDirector.Value) str += ">Combat Director: <style=cIsHealth>Resourceful</style>\n";
                    if (Inferno.Main.LevelDiffBoost.Value > 0f) str += ">Starting Difficulty: <style=cIsHealth>Increased</style>\n";
                    if (Inferno.Main.EnableSkills.Value || Inferno.Main.EnableStats.Value) str += ">Enemy Abilities: <style=cIsHealth>Improved</style>\n";
                    if (Inferno.Main.EnableAI.Value) str += ">Enemy AI: <style=cIsHealth>Refined" + (Inferno.Main.AIScaling.Value > 0f ? " + Evolving</style>\n" : "</style>\n");
                    if (Inferno.Main.MonsterLimit.Value != 40f) str += ">Enemy Cap: <style=cIsHealth>" + (Inferno.Main.MonsterLimit.Value > 40f ? "+" : "") + ((Inferno.Main.MonsterLimit.Value - 40f) * 2.5f) + "%</style>\n";
                    if (Inferno.Main.AllyPermanentDamage.Value > 0f) str += ">Allies receive <style=cIsHealth>permanent damage</style>\n";
                    return orig(self, token).Replace("{INFERNO}", str + "</style>");
                }
                return orig(self, token);
            };
        }

        public static List<string> extraSpaces = new() { "INFERNO_NAME", "BRIMSTONE_NAME", "CALYPSO_NAME", "TEMPEST_NAME", "SCYLLA_NAME" };
        public static string GetDescription(string orig, DifficultyDef def)
        {
            bool simulacrum = (PreGameController.instance?.gameModeIndex != null) && GameModeCatalog.GetGameModeName(PreGameController.instance.gameModeIndex).Contains("InfiniteTowerRun");
            if (!Hooks.Enabled(def, simulacrum)) return orig;
            DownpourPlugin.Log.LogDebug("Applying downpour description for Difficulty " + def.nameToken);
            Match m = Regex.Match(orig.ToLower(), ">\\s*difficulty\\s*scaling:\\s*<style=\\w+>.+?</style>");
            string desc = orig;
            int idx;
            #region Make sure cStack is applied & find replacement point (mess)
            if (m.Success)
            {
                idx = m.Index;
                desc = orig.Slice(0, m.Index) + orig.Slice(m.Index + m.Length);
            }
            else
            {
                idx = orig.IndexOf("\n\n") + 2;
                if (idx == 1)
                {
                    idx = orig.IndexOf("\r\n\r\n") + 4;
                    if (idx == 3) { desc += "\n\n"; idx = desc.Length; }
                }
            }
            #endregion
            string str = "<style=cStack>";
            float baseScaling = GetBaseScaling(def); if (baseScaling != 0) str += $">Difficulty Scaling: <style={(baseScaling < 0 ? "cIsHealing" : "cIsHealth")}>{PercentFormat(baseScaling)}%</style>\n";
            float stageScaling = GetStageScaling(def, simulacrum); if (stageScaling != 0) str += $">Linear Difficulty Scaling: <style={(stageScaling < 0 ? "cIsHealing" : "cIsHealth")}>{PercentFormat(stageScaling)}% per stage</style>\n";
            float scaling = GetScaling(def, simulacrum); if (scaling != 0) str += $">Exponential Difficulty Scaling:\n    <style=cIsHealth>x115% every {scaling / 60} minute{(scaling > 60 ? "s" : "")}</style>\n";
            float tempScaling = GetTempScaling(def, simulacrum); if (tempScaling != 0) str += $">Temporary Difficulty Scaling:\n    <style=cIsHealth>x115% every {tempScaling / 60} minute{(tempScaling > 60 ? "s" : "")}, resets every stage</style>\n";
            if (extraSpaces.Contains(def.nameToken)) str = str.Substring(0, str.Length - 1); // remove trailing \n
            str += "</style>"; 
            return desc.Slice(0, idx) + str + desc.Slice(idx);
        }

        public static string PercentFormat(float val) { return (val > 0 ? "+" : "") + (val * 100).ToString(); }

        public static float GetBaseScaling(DifficultyDef def, bool simulacrum = false)
        {
            if (DownpourPlugin.BrimstoneList.Contains(def)) return (DownpourPlugin.InitialScalingBrimstone.Value - 2) * 0.5f;
            if (DownpourPlugin.DownpourList.Contains(def)) return (DownpourPlugin.InitialScalingDownpour.Value - 2) * 0.5f;
            return (def.scalingValue + DownpourPlugin.InitialScaling.Value - 2) * 0.5f;
        }
        public static float GetStageScaling(DifficultyDef def, bool simulacrum = false)
        {
            if (DownpourPlugin.BrimstoneList.Contains(def)) return DownpourPlugin.StageScalingBrimstone.Value * (simulacrum ? DownpourPlugin.SimulacrumStageScalingBrimstone.Value : 1);
            if (DownpourPlugin.DownpourList.Contains(def)) return DownpourPlugin.StageScalingDownpour.Value * (simulacrum ? DownpourPlugin.SimulacrumStageScalingDownpour.Value : 1);
            return DownpourPlugin.StageScaling.Value * (simulacrum ? DownpourPlugin.SimulacrumStageScaling.Value : 1);
        }

        public static float GetScaling(DifficultyDef def, bool simulacrum = false)
        {
            if (DownpourPlugin.BrimstoneList.Contains(def)) return GetScalingInternal(DownpourPlugin.ScalingBrimstone.Value, simulacrum, DownpourPlugin.SimulacrumBaseBrimstone.Value, DownpourPlugin.SimulacrumScalingBrimstone.Value);
            if (DownpourPlugin.DownpourList.Contains(def)) return GetScalingInternal(DownpourPlugin.ScalingDownpour.Value, simulacrum, DownpourPlugin.SimulacrumBaseDownpour.Value, DownpourPlugin.SimulacrumScalingDownpour.Value);
            if (def.nameToken == "INFERNO_NAME") return GetScalingInternal(DownpourPlugin.ScalingInferno.Value, simulacrum, DownpourPlugin.SimulacrumBase.Value, DownpourPlugin.SimulacrumScaling.Value);

            float mult = (DownpourPlugin.ScalingDrizzle.Value - DownpourPlugin.ScalingMonsoon.Value) / 2;
            float init = DownpourPlugin.ScalingDrizzle.Value + mult;
            return GetScalingInternal(Mathf.Max(DownpourPlugin.ScalingMax.Value, init - (mult * def.scalingValue)), simulacrum, DownpourPlugin.SimulacrumBase.Value, DownpourPlugin.SimulacrumScaling.Value);
        }
        public static float GetScalingInternal(float orig, bool simulacrum, float simulinit, float simulmult)
        {
            if (simulacrum) return (orig + simulinit) * simulmult;
            return orig;
        }
        public static float GetTempScaling(DifficultyDef def, bool simulacrum = false)
        {
            if (DownpourPlugin.BrimstoneList.Contains(def)) return GetScalingInternal(DownpourPlugin.ScalingBrimstone.Value, simulacrum, DownpourPlugin.SimulacrumBaseBrimstone.Value, DownpourPlugin.SimulacrumScalingBrimstone.Value) * DownpourPlugin.TempScalingBrimstone.Value * (simulacrum ? DownpourPlugin.SimulacrumTempScalingBrimstone.Value : 1);
            if (DownpourPlugin.DownpourList.Contains(def)) return GetScalingInternal(DownpourPlugin.ScalingDownpour.Value, simulacrum, DownpourPlugin.SimulacrumBaseDownpour.Value, DownpourPlugin.SimulacrumScalingDownpour.Value) * DownpourPlugin.TempScalingDownpour.Value * (simulacrum ? DownpourPlugin.SimulacrumTempScalingDownpour.Value : 1);
            if (def.nameToken == "INFERNO_NAME") return GetScalingInternal(DownpourPlugin.ScalingInferno.Value, simulacrum, DownpourPlugin.SimulacrumBase.Value, DownpourPlugin.SimulacrumScaling.Value) * DownpourPlugin.TempScaling.Value * (simulacrum ? DownpourPlugin.SimulacrumTempScaling.Value : 1);

            float mult = (DownpourPlugin.ScalingDrizzle.Value - DownpourPlugin.ScalingMonsoon.Value) / 2;
            float init = DownpourPlugin.ScalingDrizzle.Value + mult;
            return GetScalingInternal(Mathf.Max(DownpourPlugin.ScalingMax.Value, init - (mult * def.scalingValue)), simulacrum, DownpourPlugin.SimulacrumBase.Value, DownpourPlugin.SimulacrumScaling.Value) * DownpourPlugin.TempScaling.Value * (simulacrum ? DownpourPlugin.SimulacrumTempScaling.Value : 1);
        }
    }
    public static class Extension
    {
        public static string Slice(this string orig, int begin, int length = -1) // js looking ass code
        {
            if (begin >= orig.Length) return "";
            else begin = ((begin % orig.Length) + orig.Length) % orig.Length;
            if (length < 0 || begin + length >= orig.Length) return orig.Substring(begin);
            else return orig.Substring(begin, length);
        }
    }
}
