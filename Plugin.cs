using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
namespace QolComboFix
{
    [BepInPlugin("qol.combofix", "QoL Combo Fix", "1.1.5")]
    [BepInDependency("com.offiry.qol", BepInDependency.DependencyFlags.SoftDependency)]
    public class QolComboFixPlugin : BaseUnityPlugin
    {
        public static QolComboFixPlugin Instance;
        public static ManualLogSource ModLogger;
        public static ConfigEntry<bool>   Cfg_Navex_WakeRecenter_Enable;
        public static ConfigEntry<float>  Cfg_Navex_WakeOffsetX;
        public static ConfigEntry<string> Cfg_Navex_WakeExcludeTokens;
        public static ConfigEntry<bool>   Cfg_Navex_VerboseLog;
        public static ConfigEntry<bool>   Cfg_Navex_AirbaseRadiusClamp_Enable;
        public static ConfigEntry<bool> Cfg_Aryx_CatapultIntegration_Enable;
        public static ConfigEntry<bool> Cfg_Catapult_Logger_Enable;
        public static ConfigEntry<bool> Cfg_Extra_Tailhook_Enable;
        public static ConfigEntry<bool> Cfg_Extra_Tailhook_Vanilla_Enable;
        public static ConfigEntry<bool> Cfg_Extra_Tailhook_Brawler_Enable;
        public static ConfigEntry<bool> Cfg_Extra_Tailhook_Ternion_Enable;
        public static ConfigEntry<bool> Cfg_Extra_Tailhook_VerboseLog;
        public static ConfigEntry<bool> Cfg_GunControl_Enable;
        public static ConfigEntry<bool> Cfg_GunControl_FixTrajectoryTrace;
        public static ConfigEntry<bool> Cfg_GunControl_FixTrajectorySim;
        public static ConfigEntry<bool> Cfg_IRplus_EnableEnhancedTurning;
        public static ConfigEntry<float> Cfg_IRplus_MissileMaxTurnRate;
        public static ConfigEntry<float> Cfg_IRplus_MissileTorqueMultiplier;
        public static ConfigEntry<bool> Cfg_IRplus_EnableLOAL;
        public static ConfigEntry<bool> Cfg_IRplus_UsePeakIRThreshold;
        public static ConfigEntry<float> Cfg_IRplus_LOALSearchTime;
        public static ConfigEntry<bool> Cfg_IRplus_EnableViewSlaving;
        public static ConfigEntry<float> Cfg_IRplus_LOALSearchAngle;
        public static ConfigEntry<bool> Cfg_IRplus_EnableHighOffBoresight;
        public static ConfigEntry<float> Cfg_IRplus_OffBoresightAngle;
        private void Awake()
        {
            Instance  = this;
            ModLogger = base.Logger;
            LogSelfLocationDiagnostics();
            BindConfigs();
            var harmony = new Harmony("qol.combofix");
            harmony.PatchAll();
            AryxCatapultIntegration.Initialize();
            ModLogger.LogInfo("QoL Combo Fix loaded.");
        }
        private void LogSelfLocationDiagnostics()
        {
            try
            {
                string asmPath = Assembly.GetExecutingAssembly().Location;
                ModLogger.LogInfo($"[QolComboFix] Loaded from: {asmPath}");
                string pluginsDir = Paths.PluginPath;
                string bepinRoot  = Directory.GetParent(pluginsDir)?.FullName;
                string fileName   = Path.GetFileName(asmPath);
                string strayPath = bepinRoot != null ? Path.Combine(bepinRoot, fileName) : null;
                if (strayPath != null && File.Exists(strayPath) && !string.Equals(strayPath, asmPath, StringComparison.OrdinalIgnoreCase))
                    ModLogger.LogWarning($"[QolComboFix] Found a stray copy at: {strayPath} (loaded copy is: {asmPath})");
            }
            catch (Exception ex)
            {
                ModLogger.LogWarning("[QolComboFix] Self-location diagnostics failed: " + ex.Message);
            }
        }
        private void BindConfigs()
        {
            const string S_ARYX        = "Aryx";
            const string S_LOGGER      = "Logger";
            Cfg_Navex_WakeRecenter_Enable = Config.Bind(S_ARYX, "WakeRecenter_Enable", true, "Center ship wakes properly.");
            Cfg_Navex_WakeOffsetX = Config.Bind(S_ARYX, "WakeOffsetX", 0f, "X offset for wakes.");
            Cfg_Navex_WakeExcludeTokens = Config.Bind(S_ARYX, "WakeRecenter_ExcludeTokens", "Styx", "Tokens to exclude.");
            Cfg_Navex_VerboseLog = Config.Bind(S_ARYX, "VerboseLog", false, "Verbose logging.");
            Cfg_Navex_AirbaseRadiusClamp_Enable = Config.Bind(S_ARYX, "Airbase Radius Clamp (Vanilla Bug)", true,
                "Clamps Airbase.GetRadius() to the attached ship's actual length so a carrier's airbase 'radius' doesn't cause overlapping ATC zones.");
            Cfg_Aryx_CatapultIntegration_Enable = Config.Bind(S_ARYX, "Penumbra Catapult Integration", true,
                "Allows Ternion/Kestrel to launch from Penumbra, and ensures aircraft spawned in Penumbra elevators/hangar 1 correctly assign to their respective catapults.");
            Cfg_Catapult_Logger_Enable = Config.Bind(S_LOGGER, "Catapult_Logger_Enable", false,
                "Enable diagnostic logging for Catapult parts positions.");
            Cfg_Extra_Tailhook_VerboseLog = Config.Bind(S_LOGGER, "Tailhook_VerboseLog", false,
                "Enable step-by-step diagnostic logging for the tailhook patch (command application, loadout padding, TailHook.Start resolution).");
            const string S_EXTRA       = "Extra";
            Cfg_Extra_Tailhook_Enable = Config.Bind(S_EXTRA, "Tailhook_Enable", true,
                "Master switch for all tailhook features.");
            Cfg_Extra_Tailhook_Vanilla_Enable = Config.Bind(S_EXTRA, "Tailhook_Vanilla_Enable", true,
                "Adjusts deployed angle for vanilla tailhooks so they catch wires better.");
            Cfg_Extra_Tailhook_Brawler_Enable = Config.Bind(S_EXTRA, "Tailhook_Brawler_Enable", true,
                "Adds a functional tailhook mount to (Brawler) so it can land on carriers.");
            Cfg_Extra_Tailhook_Ternion_Enable = Config.Bind(S_EXTRA, "Tailhook_Ternion_Enable", true,
                "Adds a functional tailhook mount to (Ternion) so it can land on carriers. (AI will still have difficulty landing because of the tall landing gear profile of the Ternion)");
            const string S_GUNCONTROL = "GunControl";
            Cfg_GunControl_Enable = Config.Bind(S_GUNCONTROL, "Enable", true, "Master switch for GunControl features");
            Cfg_GunControl_FixTrajectoryTrace = Config.Bind(S_GUNCONTROL, "FixTrajectoryTrace", true, "Fix bullet trajectory trace");
            Cfg_GunControl_FixTrajectorySim = Config.Bind(S_GUNCONTROL, "FixTrajectorySim", true, "Fix bullet trajectory simulation");
            const string S_IRPLUS = "IRplus";
            Cfg_IRplus_EnableEnhancedTurning = Config.Bind(S_IRPLUS, "EnableEnhancedTurning", true, "Enable Enhanced Turning for IR missiles");
            Cfg_IRplus_MissileMaxTurnRate = Config.Bind(S_IRPLUS, "MissileMaxTurnRate", 60f, "Max turn rate for IR missiles");
            Cfg_IRplus_MissileTorqueMultiplier = Config.Bind(S_IRPLUS, "MissileTorqueMultiplier", 2f, "Torque multiplier for IR missiles");
            Cfg_IRplus_EnableLOAL = Config.Bind(S_IRPLUS, "EnableLOAL", true, "Enable Lock-On After Launch for IR missiles");
            Cfg_IRplus_UsePeakIRThreshold = Config.Bind(S_IRPLUS, "UsePeakIRThreshold", true, "Use peak IR threshold instead of at-evasion threshold");
            Cfg_IRplus_LOALSearchTime = Config.Bind(S_IRPLUS, "LOALSearchTime", 10f, "Time in seconds to search for a target after launch");
            Cfg_IRplus_EnableViewSlaving = Config.Bind(S_IRPLUS, "EnableViewSlaving", true, "Slave seeker to player view during LOAL");
            Cfg_IRplus_LOALSearchAngle = Config.Bind(S_IRPLUS, "LOALSearchAngle", 30f, "Search angle for LOAL");
            Cfg_IRplus_EnableHighOffBoresight = Config.Bind(S_IRPLUS, "EnableHighOffBoresight", true, "Enable high off-boresight for IR missiles");
            Cfg_IRplus_OffBoresightAngle = Config.Bind(S_IRPLUS, "OffBoresightAngle", 45f, "Off-boresight angle for IR missiles");
        }
    }
}