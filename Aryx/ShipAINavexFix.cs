using HarmonyLib;
using UnityEngine;
namespace QolComboFix
{
    [HarmonyPatch(typeof(ShipAI), "Awake")]
    public static class ShipAI_Awake_ZeroStandoff_Patch
    {
        public static void Postfix(ShipAI __instance)
        {
            var standoffField = AccessTools.Field(typeof(ShipAI), "standoffDistance");
            if (standoffField != null)
            {
                float oldDistance = (float)standoffField.GetValue(__instance);
                standoffField.SetValue(__instance, 0f);
                var shipField = AccessTools.Field(typeof(ShipAI), "ship");
                string shipName = "Unknown Ship";
                if (shipField != null)
                {
                    Ship ship = (Ship)shipField.GetValue(__instance);
                    if (ship != null)
                    {
                        shipName = ship.gameObject.name;
                    }
                }
                QolComboFixPlugin.ModLogger.LogInfo($"[ShipAINavexFix] Zeroed standoffDistance for {shipName} (was {oldDistance})");
            }
        }
    }
}