using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
namespace QolComboFix
{
    public static class AryxCatapultIntegration
    {
        private static bool _initialized = false;
        private static MethodInfo _loadFromTextMethod = null;
        public static Assembly AryxAssembly = null;
        public static MethodInfo ReserveMethod = null;
        public static MethodInfo GetOrCreateMethod = null;
        public static Type UsageType = null;
        public static FieldInfo ByUnitField = null;
        public static void Initialize() 
        { 
            if (_initialized) return;
            Type dbType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                dbType = assembly.GetType("Aryx_NavalInterceptor1.AryxCatapultPatchDatabase");
                if (dbType != null)
                {
                    AryxAssembly = assembly;
                    Type registryType = assembly.GetType("Aryx_NavalInterceptor1.AryxCatapultRegistry");
                    if (registryType != null) {
                        GetOrCreateMethod = AccessTools.Method(registryType, "GetOrCreate");
                        ReserveMethod = AccessTools.Method(registryType, "Reserve");
                        UsageType = assembly.GetType("Aryx_NavalInterceptor1.AryxCatapultUsage");
                        ByUnitField = AccessTools.Field(registryType, "byUnit");
                    }
                    break;
                }
            }
            if (dbType != null)
            {
                _loadFromTextMethod = AccessTools.Method(dbType, "LoadFromText", new Type[] { typeof(string), typeof(string), typeof(bool) });
            }
            _initialized = true;
        }
        public static void InjectData(string catapultsData)
        {
            if (!_initialized) Initialize();
            if (_loadFromTextMethod != null)
            {
                _loadFromTextMethod.Invoke(null, new object[] { catapultsData, "QolComboFix Dynamic", false });
            }
        }
    }
    [HarmonyPatch(typeof(Aircraft), "Awake")]
    public class Aircraft_Awake_CatapultPatch
    {
        private static bool injectedKestrel = false;
        private static bool injectedTernion = false;
        public static void Prefix(Aircraft __instance)
        {
            if (__instance.definition == null) return;
            string aircraftName = __instance.definition.name;
            if (string.IsNullOrEmpty(aircraftName)) return;
            if (!injectedKestrel && aircraftName.IndexOf("kestrel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string catapultsData = $"{aircraftName} | fuselage_F | 0, -2.25, 0.62 | 4.0 | 90 | 5 | 0.5 | true\n";
                AryxCatapultIntegration.InjectData(catapultsData);
                injectedKestrel = true;
            }
            else if (!injectedTernion && (aircraftName.IndexOf("P_Trisurface1", StringComparison.OrdinalIgnoreCase) >= 0 || aircraftName.IndexOf("ternion", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                string catapultsData = $"{aircraftName} | cockpit | 0, -2.55, -0.25 | 4.0 | 90 | 5 | 0.5 | true\n";
                AryxCatapultIntegration.InjectData(catapultsData);
                injectedTernion = true;
            }
            if (QolComboFixPlugin.Cfg_Catapult_Logger_Enable.Value)
            {
                LogAircraftPartsAndClosestToGear(__instance, aircraftName);
            }
        }
        private static void LogAircraftPartsAndClosestToGear(Aircraft aircraft, string aircraftName)
        {
            QolComboFixPlugin.ModLogger.LogInfo($"\n--- {aircraftName} Parts Global Pos ---");
            Transform[] parts = aircraft.GetComponentsInChildren<Transform>(true);
            Transform gearF = null;
            foreach (Transform t in parts)
            {
                string tName = t.name; 
                if (tName.Equals("gear_f", StringComparison.OrdinalIgnoreCase))
                {
                    gearF = t;
                }
                QolComboFixPlugin.ModLogger.LogInfo($"{tName}: {t.position.x:F3}, {t.position.y:F3}, {t.position.z:F3}");
            }
            if (gearF != null)
            {
                Vector3 gearPos = gearF.position;
                QolComboFixPlugin.ModLogger.LogInfo($"\n--- Analyzing closest part to gear_f ({gearPos.x:F3}, {gearPos.y:F3}, {gearPos.z:F3}) for {aircraftName} ---");
                Transform closestPart = null;
                float closestDistSq = float.MaxValue;
                foreach (Transform t in parts)
                {
                    string tName = t.name;
                    if (t == gearF || 
                        tName.IndexOf("gear", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        tName.IndexOf("wheel", StringComparison.OrdinalIgnoreCase) >= 0 ) 
                    {
                        continue;
                    }
                    Vector3 tPos = t.position;
                    float dx = tPos.x - gearPos.x;
                    float dz = tPos.z - gearPos.z;
                    float distSq = dx * dx + dz * dz;
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestPart = t;
                    }
                }
                if (closestPart != null)
                {
                    Vector3 cpPos = closestPart.position;
                    float dx = cpPos.x - gearPos.x;
                    float dy = cpPos.y - gearPos.y;
                    float dz = cpPos.z - gearPos.z;
                    string cpName = closestPart.name;
                    QolComboFixPlugin.ModLogger.LogInfo($"*** HIGHLIGHT: Closest part (X/Z) to gear_f is '{cpName}' ***");
                    QolComboFixPlugin.ModLogger.LogInfo($"Pos: {cpPos.x:F3}, {cpPos.y:F3}, {cpPos.z:F3}");
                    QolComboFixPlugin.ModLogger.LogInfo($"Relative Offset (gear_f - {cpName}): {-dx:F3}, {-dy:F3}, {-dz:F3}");
                    string baseName = aircraftName;
                    if (baseName.EndsWith("_definition", StringComparison.OrdinalIgnoreCase))
                    {
                        baseName = baseName.Substring(0, baseName.Length - 11);
                    }
                    QolComboFixPlugin.ModLogger.LogInfo($"Suggested catapult config string format: {baseName} | {cpName} | 0, {-dy:F3}, {-dz:F3} | 4.0 | 90 | 5 | 0.5 | true");
                }
                else
                {
                    QolComboFixPlugin.ModLogger.LogInfo("Could not find any suitable part to compare against gear_f.");
                }
            }
            else
            {
                QolComboFixPlugin.ModLogger.LogInfo($"gear_f not found in {aircraftName}. Cannot calculate closest part.");
            }
        }
    }
    [HarmonyPatch(typeof(Hangar), "SpawnAircraft")]
    internal static class PenumbraCatapultFix
    {
        private static MethodInfo tryPatchAircraftMethod = null;
        private static bool initialized = false;
        private static Type spawnControlType = null;
        private static MethodInfo resolveHangarContextMethod = null;
        private static MethodInfo getRelativePathMethod = null;
        private static FieldInfo hangarMetadataField = null;
        private static MethodInfo getCatapultNumberMethod = null;
        public static bool VerboseLogging = true;
        private static void InitReflection()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                var harmony = new Harmony("com.qol.combo.penumbrafix");
                getCatapultNumberMethod = AccessTools.Method(AccessTools.TypeByName("Aryx_NavalInterceptor1.AryxAircraftCatapult"), "GetCatapultNumber");
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type patcherType = assembly.GetType("Aryx_NavalInterceptor1.AryxVanillaCatapultPatcher");
                    if (patcherType != null)
                    {
                        tryPatchAircraftMethod = AccessTools.Method(patcherType, "TryPatchAircraft", new Type[] { typeof(Aircraft) });
                        if (tryPatchAircraftMethod != null)
                        {
                            QolComboFixPlugin.ModLogger.LogInfo("[PenumbraFix] Found AryxVanillaCatapultPatcher.TryPatchAircraft");
                        }
                    }
                    Type spawnControl = assembly.GetType("SpawnControl.SpawnControlPlugin");
                    if (spawnControl != null)
                    {
                        spawnControlType = spawnControl;
                        resolveHangarContextMethod = AccessTools.Method(spawnControl, "ResolveHangarConfigContext", new Type[] { typeof(Hangar), typeof(string).MakeByRefType(), typeof(Transform).MakeByRefType() });
                        getRelativePathMethod = AccessTools.Method(spawnControl, "GetRelativePath", new Type[] { typeof(Transform), typeof(Transform) });
                        hangarMetadataField = AccessTools.Field(spawnControl, "HangarMetadataByPath");
                        QolComboFixPlugin.ModLogger.LogInfo("[PenumbraFix] Found SpawnControl.SpawnControlPlugin for auditor linking.");
                    }
                }
            }
            catch (Exception ex)
            {
                QolComboFixPlugin.ModLogger.LogWarning($"[PenumbraFix] Error during reflection init: {ex}");
            }
        }
        public static string GetRelativePathVerbatim(Transform t, Transform root)
        {
            if (t == null || root == null) return "";
            if (t == root) return t.name;
            string path = t.name;
            while (t.parent != null && t.parent != root)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
        [HarmonyPostfix]
        private static void Postfix(Hangar __instance, ref GameObject ___spawnedObject)
        {
            if (!QolComboFixPlugin.Cfg_Aryx_CatapultIntegration_Enable.Value) return;
            if (__instance == null || ___spawnedObject == null) return;
            Aircraft aircraft = ___spawnedObject.GetComponent<Aircraft>();
            if (aircraft == null || aircraft.disabled) return;
            if (!initialized)
            {
                InitReflection();
            }
            try
            {
                if (tryPatchAircraftMethod != null)
                {
                    tryPatchAircraftMethod.Invoke(null, new object[] { aircraft });
                }
                Hangar closestHangar = __instance;
                if (closestHangar != null)
                {
                    Unit parentUnit = closestHangar.attachedUnit ?? closestHangar.GetComponentInParent<Unit>();
                    if (parentUnit != null && parentUnit.definition != null)
                    {
                        bool isPenumbra = (parentUnit.definition.unitName != null && parentUnit.definition.unitName.IndexOf("Penumbra", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                          (parentUnit.definition.name != null && parentUnit.definition.name.IndexOf("Aryx_Supercarrier1", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (isPenumbra)
                        {
                            string hName = closestHangar.name;
                            string relativePath = GetRelativePathVerbatim(closestHangar.transform, parentUnit.transform);
                            string displayName = "";
                            if (hangarMetadataField != null && getRelativePathMethod != null && resolveHangarContextMethod != null)
                            {
                                object[] args = new object[] { closestHangar, null, null };
                                bool resolved = (bool)resolveHangarContextMethod.Invoke(null, args);
                                if (resolved)
                                {
                                    string unitName = (string)args[1];
                                    Transform rootTransform = (Transform)args[2];
                                    string spPath = (string)getRelativePathMethod.Invoke(null, new object[] { closestHangar.transform, rootTransform });
                                    var metaByPath = hangarMetadataField.GetValue(null) as System.Collections.IDictionary;
                                    if (metaByPath != null && metaByPath.Contains(unitName))
                                    {
                                        var pathDict = metaByPath[unitName] as System.Collections.IDictionary;
                                        if (pathDict != null && pathDict.Contains(spPath))
                                        {
                                            object info = pathDict[spPath];
                                            if (info != null)
                                            {
                                                FieldInfo configNameField = AccessTools.Field(info.GetType(), "ConfigName");
                                                if (configNameField != null)
                                                {
                                                    displayName = (string)configNameField.GetValue(info);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (string.IsNullOrEmpty(displayName))
                            {
                                if (relativePath.EndsWith("Hangar_F_R_01", StringComparison.OrdinalIgnoreCase)) displayName = "hangar_1";
                                else if (relativePath.EndsWith("Hangar_H_01", StringComparison.OrdinalIgnoreCase)) displayName = "elevator_3";
                            }
                            if (VerboseLogging)
                            {
                                QolComboFixPlugin.ModLogger.LogInfo($"[PenumbraFix] Aircraft '{aircraft.name}' spawned on Penumbra at '{relativePath}'. Resolved Config Name: '{displayName}'");
                            }
                            if (displayName == "hangar_1" || displayName == "elevator_3" || displayName == "elevator_2" || hName.IndexOf("hangar_1", StringComparison.OrdinalIgnoreCase) >= 0 || hName.IndexOf("elevator_3", StringComparison.OrdinalIgnoreCase) >= 0 || hName.IndexOf("elevator_2", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string targetCatNum = "3";
                                if (displayName == "elevator_2" || hName.IndexOf("elevator_2", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    targetCatNum = "2";
                                }
                                if (AryxCatapultIntegration.GetOrCreateMethod != null && AryxCatapultIntegration.ReserveMethod != null && AryxCatapultIntegration.ByUnitField != null)
                                {
                                    object registryInstance = AryxCatapultIntegration.GetOrCreateMethod.Invoke(null, null);
                                    if (registryInstance != null)
                                    {
                                        var dict = AryxCatapultIntegration.ByUnitField.GetValue(registryInstance) as System.Collections.IDictionary;
                                        if (dict != null && dict.Contains(parentUnit))
                                        {
                                            var cats = dict[parentUnit] as System.Collections.IEnumerable;
                                            if (cats != null)
                                            {
                                                object targetCat = null;
                                                foreach (var c in cats)
                                                {
                                                    if (c != null)
                                                    {
                                                        if (getCatapultNumberMethod == null)
                                                        {
                                                            getCatapultNumberMethod = AccessTools.Method(c.GetType(), "GetCatapultNumber");
                                                        }
                                                        string catNum = getCatapultNumberMethod?.Invoke(c, null) as string;
                                                        if (VerboseLogging)
                                                        {
                                                            QolComboFixPlugin.ModLogger.LogInfo($"[PenumbraFix] Found catapult: Name='{((MonoBehaviour)c).gameObject.name}', Number='{catNum}'");
                                                        }
                                                        if (catNum != null && catNum.IndexOf(targetCatNum, StringComparison.OrdinalIgnoreCase) >= 0)
                                                        {
                                                            targetCat = c;
                                                        }
                                                    }
                                                }
                                                if (targetCat != null)
                                                {
                                                    ((MonoBehaviour)targetCat).gameObject.SetActive(true);
                                                    FieldInfo allowAIField = AccessTools.Field(targetCat.GetType(), "allowAI");
                                                    if (allowAIField != null) allowAIField.SetValue(targetCat, true);
                                                    FieldInfo allowedHangarsField = AccessTools.Field(targetCat.GetType(), "allowedAISpawningHangars");
                                                    if (allowedHangarsField != null) allowedHangarsField.SetValue(targetCat, new Hangar[0]);
                                                    object usage = Activator.CreateInstance(AryxCatapultIntegration.UsageType, targetCat);
                                                    AryxCatapultIntegration.ReserveMethod.Invoke(registryInstance, new object[] { aircraft, usage });
                                                    if (VerboseLogging) QolComboFixPlugin.ModLogger.LogInfo($"[PenumbraFix] SUCCESS: Activated and Reserved Catapult {targetCatNum} for aircraft at {hName}.");
                                                }
                                                else
                                                {
                                                    QolComboFixPlugin.ModLogger.LogWarning($"[PenumbraFix] ERROR: Could not find Catapult 3 on Penumbra!");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            QolComboFixPlugin.ModLogger.LogWarning($"[PenumbraFix] Parent unit not found in AryxCatapultRegistry.byUnit dict!");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                QolComboFixPlugin.ModLogger.LogError($"[PenumbraFix] Error in Start postfix: {ex}");
            }
        }
    }
}