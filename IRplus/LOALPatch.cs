using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace QolComboFix
{
    [HarmonyPatch]
    public static class LOALPatch
    {
        private static readonly Dictionary<int, float> loalSearchStart = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> lastScanTime = new Dictionary<int, float>();
        private static readonly Dictionary<int, Dictionary<PersistentID, float>> peakObservedIR
            = new Dictionary<int, Dictionary<PersistentID, float>>();
        private static readonly Dictionary<int, Dictionary<PersistentID, float>> flareEvadedUnits
            = new Dictionary<int, Dictionary<PersistentID, float>>();
        private static readonly AccessTools.FieldRef<IRSeeker, Missile> MissileRef =
            AccessTools.FieldRefAccess<IRSeeker, Missile>("missile");
        private static readonly AccessTools.FieldRef<IRSeeker, IRSource> IRTargetRef =
            AccessTools.FieldRefAccess<IRSeeker, IRSource>("IRTarget");
        private static readonly AccessTools.FieldRef<IRSeeker, Unit> TargetUnitRef =
            AccessTools.FieldRefAccess<IRSeeker, Unit>("targetUnit");
        private static readonly AccessTools.FieldRef<IRSeeker, bool> GuidanceRef =
            AccessTools.FieldRefAccess<IRSeeker, bool>("guidance");
        private static readonly AccessTools.FieldRef<IRSeeker, Vector3> DriftErrorRef =
            AccessTools.FieldRefAccess<IRSeeker, Vector3>("driftError");
        private static readonly AccessTools.FieldRef<IRSeeker, float> DazzleAmountRef =
            AccessTools.FieldRefAccess<IRSeeker, float>("dazzleAmount");
        private static readonly AccessTools.FieldRef<IRSeeker, bool> AchievedLockRef =
            AccessTools.FieldRefAccess<IRSeeker, bool>("achievedLock");
        private static readonly AccessTools.FieldRef<IRSeeker, float> SelfDestructAtSpeedRef =
            AccessTools.FieldRefAccess<IRSeeker, float>("selfDestructAtSpeed");
        private static readonly AccessTools.FieldRef<Unit, List<IRSource>> IRSourcesRef =
            AccessTools.FieldRefAccess<Unit, List<IRSource>>("IRSources");
        private static float GetTotalIRIntensity(Unit unit)
        {
            if (unit == null) return 0f;
            var sources = IRSourcesRef(unit);
            if (sources == null) return 0f;
            float total = 0f;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null && !sources[i].flare)
                    total += sources[i].intensity;
            }
            return total;
        }
        private static void CleanupMissile(int id)
        {
            loalSearchStart.Remove(id);
            lastScanTime.Remove(id);
            peakObservedIR.Remove(id);
            flareEvadedUnits.Remove(id);
        }
        [HarmonyPatch(typeof(IRSeeker), "Initialize")]
        [HarmonyPostfix]
        public static void IRSeeker_Initialize_Postfix(IRSeeker __instance)
        {
            if (!QolComboFixPlugin.Cfg_IRplus_EnableLOAL.Value) return;
            var missile = MissileRef(__instance);
            if (missile == null) return;
            int id = missile.GetInstanceID();
            loalSearchStart[id] = Time.timeSinceLevelLoad;
            lastScanTime[id] = 0f;
            peakObservedIR[id] = new Dictionary<PersistentID, float>();
            flareEvadedUnits[id] = new Dictionary<PersistentID, float>();
        }
        [HarmonyPatch(typeof(IRSeeker), "Seek")]
        [HarmonyPostfix]
        public static void IRSeeker_Seek_Postfix(IRSeeker __instance)
        {
            if (!QolComboFixPlugin.Cfg_IRplus_EnableLOAL.Value) return;
            var missile = MissileRef(__instance);
            if (missile == null || missile.disabled) return;
            int id = missile.GetInstanceID();
            var irTarget = IRTargetRef(__instance);
            var targetUnit = TargetUnitRef(__instance);
            if (irTarget != null && irTarget.transform != null && targetUnit != null)
            {
                if (QolComboFixPlugin.Cfg_IRplus_UsePeakIRThreshold.Value)
                {
                    Dictionary<PersistentID, float> peaks;
                    if (peakObservedIR.TryGetValue(id, out peaks))
                    {
                        float currentIR = GetTotalIRIntensity(targetUnit);
                        PersistentID uid = targetUnit.persistentID;
                        float existing;
                        if (!peaks.TryGetValue(uid, out existing) || currentIR > existing)
                            peaks[uid] = currentIR;
                    }
                }
                return;
            }
            bool guidance = GuidanceRef(__instance);
            if (!guidance) return;
            if (!loalSearchStart.ContainsKey(id)) return;
            float searchElapsed = Time.timeSinceLevelLoad - loalSearchStart[id];
            if (searchElapsed > QolComboFixPlugin.Cfg_IRplus_LOALSearchTime.Value) return;
            if (QolComboFixPlugin.Cfg_IRplus_EnableViewSlaving.Value && missile.owner != null)
            {
                try
                {
                    var combatHUD = SceneSingleton<CombatHUD>.i;
                    if (combatHUD != null && combatHUD.aircraft != null
                        && combatHUD.aircraft.persistentID == missile.owner.persistentID)
                    {
                        var cam = SceneSingleton<CameraStateManager>.i;
                        if (cam != null)
                        {
                            GlobalPosition viewAimpoint = missile.GlobalPosition()
                                + cam.transform.forward * 10000f;
                            missile.SetAimpoint(viewAimpoint, Vector3.zero);
                        }
                    }
                }
                catch (Exception) {  }
            }
            if (!lastScanTime.ContainsKey(id)) lastScanTime[id] = 0f;
            if (Time.timeSinceLevelLoad - lastScanTime[id] < 0.25f) return;
            lastScanTime[id] = Time.timeSinceLevelLoad;
            Dictionary<PersistentID, float> evadedLookup = null;
            flareEvadedUnits.TryGetValue(id, out evadedLookup);
            Unit bestTarget = null;
            IRSource bestSource = null;
            float bestScore = float.MaxValue;
            float searchAngle = QolComboFixPlugin.Cfg_IRplus_LOALSearchAngle.Value;
            float maxRange = missile.GetWeaponInfo().targetRequirements.maxRange;
            Vector3 missilePos = missile.transform.position;
            Vector3 missileForward = missile.transform.forward;
            FactionHQ missileHQ = missile.NetworkHQ;
            for (int i = 0; i < UnitRegistry.allUnits.Count; i++)
            {
                Unit unit = UnitRegistry.allUnits[i];
                if (unit == null || unit.disabled) continue;
                if (unit == (Unit)missile) continue;
                if (missileHQ != null && unit.NetworkHQ == missileHQ) continue;
                if (!unit.HasIRSignature()) continue;
                Vector3 toTarget = unit.transform.position - missilePos;
                float dist = toTarget.magnitude;
                if (dist > maxRange || dist < 50f) continue;
                float angle = Vector3.Angle(missileForward, toTarget);
                if (angle > searchAngle) continue;
                if (evadedLookup != null)
                {
                    float evasionIR;
                    if (evadedLookup.TryGetValue(unit.persistentID, out evasionIR))
                    {
                        float currentIR = GetTotalIRIntensity(unit);
                        if (currentIR <= evasionIR)
                            continue;
                    }
                }
                if (Physics.Linecast(missilePos, unit.transform.position, 64))
                    continue;
                float score = angle + dist * 0.001f;
                if (score < bestScore)
                {
                    IRSource source = unit.GetIRSource();
                    if (source != null && !source.flare)
                    {
                        bestTarget = unit;
                        bestSource = source;
                        bestScore = score;
                    }
                }
            }
            if (bestTarget != null && bestSource != null)
            {
                IRTargetRef(__instance) = bestSource;
                TargetUnitRef(__instance) = bestTarget;
                DriftErrorRef(__instance) = Vector3.zero;
                DazzleAmountRef(__instance) = 0f;
                AchievedLockRef(__instance) = false;
                try
                {
                    var flareHandler = AccessTools.Method(typeof(IRSeeker), "IRSeeker_OnTargetFlare");
                    if (flareHandler != null)
                    {
                        var del = (Action<IRSource>)Delegate.CreateDelegate(
                            typeof(Action<IRSource>), __instance, flareHandler);
                        bestTarget.onAddIRSource += del;
                    }
                }
                catch (Exception) {  }
                missile.SetTarget(bestTarget);
                loalSearchStart.Remove(id);
                lastScanTime.Remove(id);
                QolComboFixPlugin.ModLogger.LogDebug(
                    $"[LOAL] Acquired lock on {bestTarget.unitName} at {bestScore:F1} score");
            }
        }
        [HarmonyPatch(typeof(IRSeeker), "IRSeeker_OnTargetFlare")]
        [HarmonyPrefix]
        public static void IRSeeker_OnTargetFlare_Prefix(
            IRSeeker __instance,
            out FlareEvasionSnapshot __state)
        {
            __state = default;
            if (!QolComboFixPlugin.Cfg_IRplus_EnableLOAL.Value) return;
            var targetUnit = TargetUnitRef(__instance);
            var irTarget = IRTargetRef(__instance);
            if (targetUnit != null && irTarget != null && !irTarget.flare)
            {
                __state = new FlareEvasionSnapshot
                {
                    valid = true,
                    unitId = targetUnit.persistentID,
                    originalIRTarget = irTarget
                };
            }
        }
        [HarmonyPatch(typeof(IRSeeker), "IRSeeker_OnTargetFlare")]
        [HarmonyPostfix]
        public static void IRSeeker_OnTargetFlare_Postfix(
            IRSeeker __instance,
            FlareEvasionSnapshot __state)
        {
            if (!QolComboFixPlugin.Cfg_IRplus_EnableLOAL.Value || !__state.valid) return;
            var missile = MissileRef(__instance);
            if (missile == null) return;
            var currentIRTarget = IRTargetRef(__instance);
            if (currentIRTarget != __state.originalIRTarget)
            {
                int missileId = missile.GetInstanceID();
                float thresholdIR = 0f;
                if (QolComboFixPlugin.Cfg_IRplus_UsePeakIRThreshold.Value)
                {
                    Dictionary<PersistentID, float> peaks;
                    if (peakObservedIR.TryGetValue(missileId, out peaks))
                        peaks.TryGetValue(__state.unitId, out thresholdIR);
                }
                if (thresholdIR <= 0f)
                {
                    Unit evadedUnit;
                    if (UnitRegistry.TryGetUnit(new PersistentID?(__state.unitId), out evadedUnit))
                        thresholdIR = GetTotalIRIntensity(evadedUnit);
                }
                if (!flareEvadedUnits.ContainsKey(missileId))
                    flareEvadedUnits[missileId] = new Dictionary<PersistentID, float>();
                flareEvadedUnits[missileId][__state.unitId] = thresholdIR;
                if (!loalSearchStart.ContainsKey(missileId))
                    loalSearchStart[missileId] = Time.timeSinceLevelLoad;
                string mode = QolComboFixPlugin.Cfg_IRplus_UsePeakIRThreshold.Value ? "peak observed" : "at evasion";
                QolComboFixPlugin.ModLogger.LogDebug(
                    $"[LOAL] Flare evasion: unit {__state.unitId}, IR threshold ({mode}): {thresholdIR:F2}. " +
                    $"Relock requires aircraft IR > {thresholdIR:F2} while in seeker cone.");
            }
        }
        [HarmonyPatch(typeof(IRSeeker), "SlowChecks")]
        [HarmonyPrefix]
        public static bool IRSeeker_SlowChecks_Prefix(IRSeeker __instance)
        {
            if (!QolComboFixPlugin.Cfg_IRplus_EnableLOAL.Value) return true;
            var missile = MissileRef(__instance);
            if (missile == null || missile.disabled) return true;
            int id = missile.GetInstanceID();
            if (!loalSearchStart.ContainsKey(id)) return true;
            float searchElapsed = Time.timeSinceLevelLoad - loalSearchStart[id];
            if (searchElapsed > QolComboFixPlugin.Cfg_IRplus_LOALSearchTime.Value)
            {
                CleanupMissile(id);
                return true;
            }
            if (missile.EngineOn()) return false;
            bool losingGround = missile.LosingGround();
            bool missedTarget = missile.MissedTarget();
            float selfDestructSpeed = SelfDestructAtSpeedRef(__instance);
            if (losingGround || missedTarget || missile.speed < selfDestructSpeed)
            {
                CleanupMissile(id);
                return true;
            }
            return false;
        }
        [HarmonyPatch(typeof(Missile), "UnitDisabled")]
        [HarmonyPostfix]
        public static void Missile_UnitDisabled_Postfix(Missile __instance)
        {
            CleanupMissile(__instance.GetInstanceID());
        }
    }
    public struct FlareEvasionSnapshot
    {
        public bool valid;
        public PersistentID unitId;
        public IRSource originalIRTarget;
    }
}