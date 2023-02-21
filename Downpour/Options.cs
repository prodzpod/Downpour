using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;

namespace Downpour
{
    public class Options
    {
        public static void Patch()
        {
            ModSettingsManager.SetModIcon(DownpourPlugin.AssetBundle.LoadAsset<Sprite>("Assets/iconDownpour.png"), DownpourPlugin.PluginGUID, DownpourPlugin.PluginName);
            AddOption(DownpourPlugin.EnableRework, true);
            AddOption(DownpourPlugin.EnableInferno, true);
            AddOption(DownpourPlugin.EnableDownpour, true);
            AddOption(DownpourPlugin.EnableBrimstone, true);
            AddOption(DownpourPlugin.FasterSimulacrum, true);

            AddOption(DownpourPlugin.ScalingDrizzle, 0, 1200, 15);
            AddOption(DownpourPlugin.ScalingMonsoon, 0, 1200, 15);
            AddOption(DownpourPlugin.ScalingInferno, 0, 1200, 15);
            AddOption(DownpourPlugin.ScalingMax, 0, 1200, 15);
            AddOption(DownpourPlugin.TempScaling, 0, 5, 0.05f);
            AddOption(DownpourPlugin.InitialScaling, 0, 5, 0.05f);
            AddOption(DownpourPlugin.StageScaling, -5, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumBase, -600, 600, 15);
            AddOption(DownpourPlugin.SimulacrumScaling, 0, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumTempScaling, 0, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumStageScaling, -5, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumCountdown, 0, 20, 1);
            AddOption(DownpourPlugin.SimulacrumBossHealth, 0, 5, 0.05f);

            AddOption(DownpourPlugin.ScalingDownpour, 0, 1200, 15);
            AddOption(DownpourPlugin.TempScalingDownpour, 0, 5, 0.05f);
            AddOption(DownpourPlugin.InitialScalingDownpour, 0, 5, 0.05f);
            AddOption(DownpourPlugin.StageScalingDownpour, -5, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumBaseDownpour, -600, 600, 15);
            AddOption(DownpourPlugin.SimulacrumScalingDownpour, 0, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumTempScalingDownpour, 0, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumStageScalingDownpour, -5, 5, 0.05f);

            AddOption(DownpourPlugin.ScalingBrimstone, 0, 1200, 15);
            AddOption(DownpourPlugin.TempScalingBrimstone, 0, 5, 0.05f);
            AddOption(DownpourPlugin.InitialScalingBrimstone, 0, 5, 0.05f);
            AddOption(DownpourPlugin.StageScalingBrimstone, -5, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumBaseBrimstone, -600, 600, 15);
            AddOption(DownpourPlugin.SimulacrumScalingBrimstone, 0, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumTempScalingBrimstone, 0, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumStageScalingBrimstone, -5, 5, 0.05f);
            AddOption(DownpourPlugin.SimulacrumCountdownDownpour, 0, 20, 1);

            if (DownpourPlugin.DEBUG) foreach (var config in DownpourPlugin.AutoAdvance) AddOption(config, 0, 20, 1);
        }

        public static void AddOption(ConfigEntry<bool> entry, bool restart = false)
        {
            ModSettingsManager.AddOption(new CheckBoxOption(entry, restart), DownpourPlugin.PluginGUID, DownpourPlugin.PluginName);
        }

        public static void AddOption(ConfigEntry<float> entry, float min, float max, float step)
        {
            StepSliderConfig config = new();
            config.max = max;
            config.min = min;
            config.increment = step;
            ModSettingsManager.AddOption(new StepSliderOption(entry, config), DownpourPlugin.PluginGUID, DownpourPlugin.PluginName);
        }
    }
}
