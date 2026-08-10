using HarmonyLib;
using UnityEngine;
namespace QolComboFix
{
    [HarmonyPatch]
    public static class EnhancedTurningPatch
    {
        [HarmonyPatch(typeof(Missile), "StartMissile")]
        [HarmonyPostfix]
        public static void Missile_StartMissile_Postfix(Missile __instance)
        {
            if (!QolComboFixPlugin.Cfg_IRplus_EnableEnhancedTurning.Value) return;
            var seeker = __instance.gameObject.GetComponent<IRSeeker>();
            if (seeker == null) return;
            float origTurnRate = __instance.GetMaxTurnRate();
            float origTorque = __instance.GetTorque();
            float newTurnRate = Mathf.Max(origTurnRate, QolComboFixPlugin.Cfg_IRplus_MissileMaxTurnRate.Value);
            float newTorque = origTorque * QolComboFixPlugin.Cfg_IRplus_MissileTorqueMultiplier.Value;
            __instance.SetTorque(newTorque, newTurnRate);
            QolComboFixPlugin.ModLogger.LogDebug($"[TVC] Enhanced IR missile turning: turnRate {origTurnRate} -> {newTurnRate}, torque {origTorque} -> {newTorque}");
        }
    }
}