using HarmonyLib;
namespace QolComboFix
{
    internal static class ATCAirbaseRadiusFix
    {
        [HarmonyPatch(typeof(Airbase), nameof(Airbase.GetRadius))]
        private static class GetRadius_ClampToShipLength
        {
            static void Postfix(Airbase __instance, ref float __result)
            {
                if (!QolComboFixPlugin.Cfg_Navex_AirbaseRadiusClamp_Enable.Value) return;
                if (__instance == null) return;
                if (!__instance.TryGetAttachedUnit(out Unit unit) || unit == null || unit.definition == null) return;
                float shipLength = unit.definition.length;
                if (shipLength > 0f && __result > shipLength)
                {
                    __result = shipLength;
                }
            }
        }
    }
}