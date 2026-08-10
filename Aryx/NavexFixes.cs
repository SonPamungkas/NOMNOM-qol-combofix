using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace QolComboFix
{
    [HarmonyPatch(typeof(Ship), "Awake")]
    public static class Ship_Awake_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Ship __instance)
        {
            try
            {
                if (__instance == null)
                    return;
                if (QolComboFixPlugin.Cfg_Navex_WakeRecenter_Enable.Value && IsNavexShip(__instance) && !NameMatchesAny(__instance, QolComboFixPlugin.Cfg_Navex_WakeExcludeTokens.Value))
                    WakeRecenter.Apply(__instance);
            }
            catch (Exception ex)
            {
                QolComboFixPlugin.ModLogger.LogError($"[Navex] Ship_Awake_Patch threw: {ex}");
            }
        }
        internal static bool IsNavexShip(Ship ship)
        {
            if (ship == null) return false;
            string defName = ship.definition != null ? ship.definition.name : null;
            if (defName != null && defName.IndexOf("Aryx", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (ship.gameObject.name.IndexOf("Aryx", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        internal static bool NameMatchesAny(Ship ship, string pipeSeparatedTokens)
        {
            if (string.IsNullOrWhiteSpace(pipeSeparatedTokens))
                return false;
            string defName = ship.definition != null ? ship.definition.name : null;
            string unitName = ship.unitName;
            string goName = ship.gameObject.name;
            foreach (string raw in pipeSeparatedTokens.Split('|'))
            {
                string token = raw?.Trim();
                if (string.IsNullOrEmpty(token))
                    continue;
                if (ContainsCI(defName, token) || ContainsCI(unitName, token) || ContainsCI(goName, token))
                    return true;
            }
            return false;
        }
        private static bool ContainsCI(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle))
                return false;
            return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
    internal static class WakeRecenter
    {
        private static readonly Type WakeParticlesType = AccessTools.Inner(typeof(Ship), "WakeParticles");
        private static readonly AccessTools.FieldRef<object, Array> WakeParticlesRef =
            AccessTools.FieldRefAccess<Array>(typeof(Ship), "wakeParticles");
        private static readonly AccessTools.FieldRef<object, ParticleSystem> SystemRef =
            WakeParticlesType != null ? AccessTools.FieldRefAccess<ParticleSystem>(WakeParticlesType, "system") : null;
        private static readonly bool ReflectionResolved = WakeParticlesType != null && SystemRef != null && WakeParticlesRef != null;
        public static void Apply(Ship ship)
        {
            if (!ReflectionResolved)
            {
                QolComboFixPlugin.ModLogger.LogError("[Navex] Could not resolve Ship.wakeParticles or WakeParticles.system — wake recenter will no-op.");
                return;
            }
            Array wakeParticles = WakeParticlesRef(ship);
            if (wakeParticles == null || wakeParticles.Length == 0)
                return;
            bool verbose = QolComboFixPlugin.Cfg_Navex_VerboseLog.Value;
            int recenteredCount = 0;
            foreach (object entry in wakeParticles)
            {
                if (entry == null)
                    continue;
                ParticleSystem system = SystemRef(entry);
                if (system == null)
                    continue;
                if (CorrectEmitter(ship, system, verbose))
                    recenteredCount++;
            }
            if (recenteredCount > 0)
                QolComboFixPlugin.ModLogger.LogInfo($"[Navex] Recentered {recenteredCount} wake emitter(s) on '{ship.unitName ?? ship.gameObject.name}'.");
        }
        public static bool CorrectEmitter(Ship ship, ParticleSystem system, bool verbose)
        {
            float targetX = QolComboFixPlugin.Cfg_Navex_WakeOffsetX.Value;
            Transform shipRoot = ship.transform;
            Transform t = system.transform;
            float currentLateral = Vector3.Dot(t.position - shipRoot.position, shipRoot.right);
            float delta = currentLateral - targetX;
            if (Mathf.Approximately(delta, 0f))
                return false;
            if (verbose)
                QolComboFixPlugin.ModLogger.LogInfo($"[Navex] Wake '{system.gameObject.name}' on '{ship.unitName ?? ship.gameObject.name}': lateral offset {currentLateral:F3} -> {targetX:F3}");
            t.position -= shipRoot.right * delta;
            return true;
        }
        public static void ReparentAndCorrect(Ship ship, ParticleSystem system, bool verbose)
        {
            Transform t = system.transform;
            if (Datum.origin != null && t.parent == Datum.origin)
            {
                t.SetParent(ship.transform, worldPositionStays: true);
                if (verbose)
                    QolComboFixPlugin.ModLogger.LogInfo($"[Navex] Reparented '{system.gameObject.name}' on '{ship.unitName ?? ship.gameObject.name}' from Datum back to ship root.");
            }
            CorrectEmitter(ship, system, verbose);
        }
    }
    [HarmonyPatch]
    internal static class WakeParticles_Update_Patch
    {
        private const BindingFlags InstFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type WakeParticlesType = AccessTools.Inner(typeof(Ship), "WakeParticles");
        private static readonly AccessTools.FieldRef<object, ParticleSystem> SystemRef =
            WakeParticlesType != null ? AccessTools.FieldRefAccess<ParticleSystem>(WakeParticlesType, "system") : null;
        private static readonly AccessTools.FieldRef<object, Unit> ParentUnitRef =
            WakeParticlesType != null ? AccessTools.FieldRefAccess<Unit>(WakeParticlesType, "parentUnit") : null;
        private static readonly bool ReflectionResolved = SystemRef != null && ParentUnitRef != null;
        private static bool _warnedMissingReflection;
        private static MethodBase TargetMethod()
        {
            return WakeParticlesType?.GetMethod("Update", InstFlags);
        }
        [HarmonyPostfix]
        private static void Postfix(object __instance, float speed, Vector3 velocity)
        {
            if (!QolComboFixPlugin.Cfg_Navex_WakeRecenter_Enable.Value)
                return;
            if (!ReflectionResolved)
            {
                if (!_warnedMissingReflection)
                {
                    _warnedMissingReflection = true;
                    QolComboFixPlugin.ModLogger.LogError("[Navex] Could not resolve WakeParticles.system / parentUnit — late wake recenter will no-op.");
                }
                return;
            }
            try
            {
                ParticleSystem system = SystemRef(__instance);
                if (system == null || system.transform.parent != Datum.origin)
                    return;
                Unit unit = ParentUnitRef(__instance);
                if (!(unit is Ship ship))
                    return;
                if (!Ship_Awake_Patch.IsNavexShip(ship))
                    return;
                if (Ship_Awake_Patch.NameMatchesAny(ship, QolComboFixPlugin.Cfg_Navex_WakeExcludeTokens.Value))
                    return;
                WakeRecenter.ReparentAndCorrect(ship, system, QolComboFixPlugin.Cfg_Navex_VerboseLog.Value);
            }
            catch (Exception ex)
            {
                QolComboFixPlugin.ModLogger.LogError("[Navex] WakeParticles_Update_Patch threw: " + ex.Message);
            }
        }
    }
}