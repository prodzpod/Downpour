using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Downpour
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInDependency("HIFU.Inferno", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Wolfo.LittleGameplayTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("BALLS.WellRoundedBalance", BepInDependency.DependencyFlags.SoftDependency)]
    public class DownpourPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "prodzpod";
        public const string PluginName = "Downpour";
        public const string PluginVersion = "1.0.2";

        public static ManualLogSource Log;
        public static Harmony Harmony;
        internal static PluginInfo pluginInfo;
        public static ConfigFile Config;
        private static AssetBundle _assetBundle;
        public static AssetBundle AssetBundle
        {
            get
            {
                if (_assetBundle == null)
                    _assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pluginInfo.Location), "downpour"));
                return _assetBundle;
            }
        }

        public static ConfigEntry<bool> EnableRework;
        public static ConfigEntry<bool> EnableInferno;
        public static ConfigEntry<bool> EnableDownpour;
        public static ConfigEntry<bool> EnableBrimstone;
        public static ConfigEntry<bool> FasterSimulacrum;
        public static ConfigEntry<float> ScalingDrizzle;
        public static ConfigEntry<float> ScalingMonsoon;
        public static ConfigEntry<float> ScalingInferno;
        public static ConfigEntry<float> ScalingMax;
        public static ConfigEntry<float> TempScaling;
        public static ConfigEntry<float> InitialScaling;
        public static ConfigEntry<float> StageScaling;
        public static ConfigEntry<float> SimulacrumBase;
        public static ConfigEntry<float> SimulacrumScaling;
        public static ConfigEntry<float> SimulacrumTempScaling;
        public static ConfigEntry<float> SimulacrumStageScaling;
        public static ConfigEntry<float> SimulacrumCountdown;
        public static ConfigEntry<float> ScalingDownpour;
        public static ConfigEntry<float> TempScalingDownpour;
        public static ConfigEntry<float> InitialScalingDownpour;
        public static ConfigEntry<float> StageScalingDownpour;
        public static ConfigEntry<float> SimulacrumBaseDownpour;
        public static ConfigEntry<float> SimulacrumScalingDownpour;
        public static ConfigEntry<float> SimulacrumTempScalingDownpour;
        public static ConfigEntry<float> SimulacrumStageScalingDownpour;
        public static ConfigEntry<float> SimulacrumCountdownDownpour;
        public static ConfigEntry<float> ScalingBrimstone;
        public static ConfigEntry<float> TempScalingBrimstone;
        public static ConfigEntry<float> InitialScalingBrimstone;
        public static ConfigEntry<float> StageScalingBrimstone;
        public static ConfigEntry<float> SimulacrumBaseBrimstone;
        public static ConfigEntry<float> SimulacrumScalingBrimstone;
        public static ConfigEntry<float> SimulacrumTempScalingBrimstone;
        public static ConfigEntry<float> SimulacrumStageScalingBrimstone;
        public static DifficultyDef Downpour;
        public static DifficultyIndex DownpourIndex;
        public static DifficultyDef Brimstone;
        public static DifficultyIndex BrimstoneIndex;
        public static List<DifficultyDef> DownpourList = new();
        public static List<DifficultyDef> BrimstoneList = new();

        public void Awake()
        {
            pluginInfo = Info;
            Log = Logger;
            Harmony = new Harmony(PluginGUID); // uh oh!
            Config = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);

            EnableRework = Config.Bind("Modules", "Enable All Difficulty Reworks", true, "Non-inferno, downpour, brimstone difficulty scaling will be reworked.");
            EnableInferno = Config.Bind("Modules", "Enable Inferno Reworks", true, "Inferno difficulty scaling will be reworked.");
            EnableDownpour = Config.Bind("Modules", "Enable Downpour Difficulty", true, "Downpour will be added.");
            EnableBrimstone = Config.Bind("Modules", "Enable Brimstone Difficulty", true, "Brimstone will be added.");
            FasterSimulacrum = Config.Bind("Modules", "Faster Simulacrum", true, "Force spawns more enemies upon all kill mid-wave.");

            ScalingDrizzle = Config.Bind("Difficulty Rework", "Drizzle Scaling Seconds", 600f, "Will be used with monsoon to calculate all difficulty.");
            ScalingMonsoon = Config.Bind("Difficulty Rework", "Monsoon Scaling Seconds", 300f, "Will be used with drizzle to calculate all difficulty.");
            ScalingInferno = Config.Bind("Difficulty Rework", "Inferno Scaling Seconds", 300f, "Special exception. set to 0 to disable.");
            ScalingMax = Config.Bind("Difficulty Rework", "Max Scaling Seconds", 300f, "Any def above it won't increase. Used to mitigate negative/zero.");
            TempScaling = Config.Bind("Difficulty Rework", "Temporary Scaling Multiplier", 1f, "Scaling value that dissipates on next stage. Lower = harder.");
            InitialScaling = Config.Bind("Difficulty Rework", "Initial Scaling", -0.5f, "Default: Monsoon starts as thunderstorm, and ends(stage 5) on typhoon");
            StageScaling = Config.Bind("Downpour", "Stage Scaling Multiplier", 0.25f, "Default: rainstorm will become monsoon on stage 5.");
            SimulacrumBase = Config.Bind("Difficulty Rework", "Simulacrum Scaling Seconds Base", 0f, "Base seconds to add/remove for simulacrum.");
            SimulacrumScaling = Config.Bind("Difficulty Rework", "Simulacrum Scaling Multiplier", 2.5f, "Multiplied to normal game scaling time. Lower = harder");
            SimulacrumTempScaling = Config.Bind("Difficulty Rework", "Simulacrum Temporary Scaling Multiplier", 0f, "Temporary scaling multiplier for simulacrum, takes simulacrum scaling.");
            SimulacrumStageScaling = Config.Bind("Difficulty Rework", "Simulacrum Stage Scaling Multiplier", 1f, "Stage scaling multiplier for simulacrum, takes simulacrum scaling.");

            SimulacrumCountdown = Config.Bind("Difficulty Rework", "Simulacrum Countdown", 3f, "Instead of the usual 10 seconds.");

            ScalingDownpour = Config.Bind("Downpour", "Downpour Scaling Seconds", 180f, "Special exception. set to 0 to disable.");
            TempScalingDownpour = Config.Bind("Downpour", "Downpour Temporary Scaling Multiplier", 0.5f, "Special exception.");
            InitialScalingDownpour = Config.Bind("Downpour", "Downpour Initial Scaling", 2f, "Default: start identical to rainstorm.");
            StageScalingDownpour = Config.Bind("Downpour", "Downpour Stage Scaling Multiplier", 0f, "Default: will become monsoon on stage 5.");
            SimulacrumBaseDownpour = Config.Bind("Downpour", "Downpour Simulacrum Scaling Base", 0f, "Special exception.");
            SimulacrumScalingDownpour = Config.Bind("Downpour", "Downpour Simulacrum Scaling Multiplier", 2.5f, "Special exception.");
            SimulacrumTempScalingDownpour = Config.Bind("Downpour", "Downpour Simulacrum Temporary Scaling Multiplier", 2f, "Special exception.");
            SimulacrumStageScalingDownpour = Config.Bind("Downpour", "Downpour Simulacrum Stage Scaling Multiplier", 1f, "Special exception.");

            SimulacrumCountdownDownpour = Config.Bind("Downpour", "Downpour Simulacrum Countdown", 0f, "Instead of the usual 10 seconds.");

            ScalingBrimstone = Config.Bind("Downpour", "Brimstone Scaling Seconds", 240f, "Special exception. set to 0 to disable.");
            TempScalingBrimstone = Config.Bind("Downpour", "Brimstone Temporary Scaling Multiplier", 0.5f, "Special exception.");
            InitialScalingBrimstone = Config.Bind("Downpour", "Brimstone Initial Scaling", 3f, "Default: start identical to rainstorm.");
            StageScalingBrimstone = Config.Bind("Downpour", "Brimstone Stage Scaling Multiplier", 0f, "Default: will become monsoon on stage 5.");
            SimulacrumBaseBrimstone = Config.Bind("Downpour", "Brimstone Simulacrum Scaling Seconds Base", 0f, "Special exception.");
            SimulacrumScalingBrimstone = Config.Bind("Downpour", "Brimstone Simulacrum Scaling Seconds Multiplier", 2.5f, "Special exception.");
            SimulacrumTempScalingBrimstone = Config.Bind("Downpour", "Brimstone Simulacrum Temporary Scaling Seconds Multiplier", 2f, "Special exception.");
            SimulacrumStageScalingBrimstone = Config.Bind("Downpour", "Brimstone Simulacrum Temporary Stage Seconds Multiplier", 1f, "Special exception.");

            if (Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions")) Options.Patch();

            Harmony.PatchAll(typeof(Hooks.PatchOrder));
            if (EnableDownpour.Value) AddDifficulty();
            if (Chainloader.PluginInfos.ContainsKey("HIFU.Inferno") && EnableBrimstone.Value) AddInfernoVariant();
            Hooks.Patch();
            Run.onRunStartGlobal += Hooks.PatchRun;
            Run.onRunDestroyGlobal -= Hooks.PatchRun;
            Run.onRunStartGlobal += Hooks.PatchSimulacrum;
            Run.onRunDestroyGlobal -= Hooks.PatchSimulacrum;
            RoR2Application.onLoad += Token.Patch;
        }

        public static void AddDifficulty()
        {
            Downpour = new(2f, "DOWNPOUR_NAME", "DOWNPOUR_ICON", "DOWNPOUR_DESC", new Color32(98, 157, 230, 255), "DP", false);
            Downpour.iconSprite = AssetBundle.LoadAsset<Sprite>("Assets/downpour.png");
            Downpour.foundIconSprite = true;
            DownpourIndex = DifficultyAPI.AddDifficulty(Downpour);
            DownpourList.Add(Downpour);
        }
        public static void AddInfernoVariant()
        {
            Brimstone = new(3f, "BRIMSTONE_NAME", "BRIMSTONE_ICON", "BRIMSTONE_DESC", new Color32(213, 145, 242, 255), "BS", false);
            Brimstone.iconSprite = AssetBundle.LoadAsset<Sprite>("Assets/brimstone.png");
            Brimstone.foundIconSprite = true;
            BrimstoneIndex = DifficultyAPI.AddDifficulty(Brimstone);
            DownpourList.Add(Brimstone);
            BrimstoneList.Add(Brimstone);
            Harmony.PatchAll(typeof(Hooks.InfernoButCringePatch));
            Harmony.PatchAll(typeof(Hooks.BrimstoneAchievementPatch));
            Token.PatchBrimstone();
        }
    }
}
