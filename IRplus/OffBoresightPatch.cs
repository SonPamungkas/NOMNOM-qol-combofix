using HarmonyLib;
using UnityEngine;
namespace QolComboFix
{
    [HarmonyPatch]
    public static class OffBoresightPatch
    {
        [HarmonyPatch(typeof(HUDMissileState), "SetHUDWeaponState")]
        [HarmonyPostfix]
        public static void HUDMissileState_SetHUDWeaponState_Postfix(HUDMissileState __instance, WeaponStation weaponStation)
        {
            if (!QolComboFixPlugin.Cfg_IRplus_EnableHighOffBoresight.Value) return;
            var info = weaponStation.WeaponInfo;
            if (info == null) return;
            if (info.targetRequirements.minIR <= 0f) return;
            var field = AccessTools.Field(typeof(HUDMissileState), "minAlignment");
            if (field != null)
            {
                field.SetValue(__instance, QolComboFixPlugin.Cfg_IRplus_OffBoresightAngle.Value);
                QolComboFixPlugin.ModLogger.LogDebug($"[HOBS] Set HUD minAlignment to {QolComboFixPlugin.Cfg_IRplus_OffBoresightAngle.Value}° for {info.weaponName}");
            }
        }
        [HarmonyPatch(typeof(CombatAI), "AnalyzeTarget")]
        [HarmonyPrefix]
        public static void CombatAI_AnalyzeTarget_Prefix(WeaponStation weaponStation)
        {
            if (!QolComboFixPlugin.Cfg_IRplus_EnableHighOffBoresight.Value) return;
            var info = weaponStation.WeaponInfo;
            if (info == null || info.targetRequirements.minIR <= 0f) return;
            var reqs = info.targetRequirements;
            if (reqs.minAlignment < QolComboFixPlugin.Cfg_IRplus_OffBoresightAngle.Value)
            {
                reqs.minAlignment = QolComboFixPlugin.Cfg_IRplus_OffBoresightAngle.Value;
                info.targetRequirements = reqs;
            }
        }
    }
}