using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using NuclearOption.SavedMission;
namespace QolComboFix.Extra
{
    [HarmonyPatch(typeof(Encyclopedia), "AfterLoad", new Type[] { })]
    public static class EncyclopediaTailhookPatch
    {
        private static string[] _qolCommands = null;
        private static bool _hasRun = false;
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (_hasRun) return;
            _hasRun = true;
            if (!QolComboFixPlugin.Cfg_Extra_Tailhook_Enable.Value) return;
            LoadCommands();
            Aircraft[] allAircraft = Resources.FindObjectsOfTypeAll<Aircraft>().Where(a => a.gameObject.scene.name == null).ToArray();
            foreach (string line in _qolCommands)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                if (line.StartsWith("CAS1", StringComparison.OrdinalIgnoreCase) && !QolComboFixPlugin.Cfg_Extra_Tailhook_Brawler_Enable.Value) continue;
                if (line.StartsWith("P_Trisurface1", StringComparison.OrdinalIgnoreCase) && !QolComboFixPlugin.Cfg_Extra_Tailhook_Ternion_Enable.Value) continue;
                if (line.StartsWith("TailHook_", StringComparison.OrdinalIgnoreCase) && !QolComboFixPlugin.Cfg_Extra_Tailhook_Vanilla_Enable.Value) continue;
                try
                {
                    Match fmMatch = FieldModifyPattern.Match(line);
                    if (fmMatch.Success)
                    {
                        ProcessFieldModify(fmMatch);
                        continue;
                    }
                    string targetName = line.Split(new[] { ' ', '/' }, 2)[0];
                    Aircraft aircraftPrefab = allAircraft.FirstOrDefault(a => a.gameObject.name.Replace("(Clone)", "").Trim().Equals(targetName, StringComparison.OrdinalIgnoreCase));
                    if (aircraftPrefab != null && aircraftPrefab.weaponManager != null)
                    {
                        ProcessCommand(line, aircraftPrefab.weaponManager, aircraftPrefab);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[QolComboFix] Failed to process line: '{line}'. Error: {ex}");
                }
            }
        }
        private static void LoadCommands()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("tailhook.commands.qol"));
                if (resourceName != null)
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();
                        _qolCommands = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                else
                {
                    Debug.LogError("[QolComboFix] Embedded resource tailhook.commands.qol not found!");
                    _qolCommands = new string[0];
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QolComboFix] Failed to load commands: {ex}");
                _qolCommands = new string[0];
            }
        }
        private static readonly Regex HardpointSetPattern = new Regex(@"^(?<path>(?:\""[^\""]+\""|\S+))\s+hardpointset\s+(?<operation>add|name|addhardpoint|modifyhardpoint|precludehardpoint)\s+(?:(?<setIndex>\d+)\s*)?(?:(?<name>(?:\""[^\""]+\""|\S+))\s*)?(?:(?<hpIndex>\d+)\s*)?(?:(?<transformPath>(?:\""[^\""]+\""|\S+))\s*)?(?:(?<partPath>(?:\""[^\""]+\""|\S+))\s*)?$", RegexOptions.Compiled);
        private static readonly Regex TransformPattern = new Regex(@"^(?<path>[^\s]+)\s+transform\s+(?<posx>[-+]?[0-9]*\.?[0-9]+)\s+(?<posy>[-+]?[0-9]*\.?[0-9]+)\s+(?<posz>[-+]?[0-9]*\.?[0-9]+)\s+(?<rotx>[-+]?[0-9]*\.?[0-9]+)\s+(?<roty>[-+]?[0-9]*\.?[0-9]+)\s+(?<rotz>[-+]?[0-9]*\.?[0-9]+)\s+(?<scalex>[-+]?[0-9]*\.?[0-9]+)\s+(?<scaley>[-+]?[0-9]*\.?[0-9]+)\s+(?<scalez>[-+]?[0-9]*\.?[0-9]+)$", RegexOptions.Compiled);
        private static readonly Regex WeaponPattern = new Regex(@"^(?<path>[^\s]+)\s+(?<operation>add|remove)weapon(?<setIndex>\d+)\s+(?<prefabName>[^\s]+)$", RegexOptions.Compiled);
        private static readonly Regex FieldModifyPattern = new Regex(@"^(?<target>[^\s]+)\s+(?<component>[^\s]+)\s+(?<field>[^\s]+)\s+modify\s+(?<value>[^\s]+)$", RegexOptions.Compiled);
        private static void ProcessCommand(string line, WeaponManager weaponManager, Aircraft aircraft)
        {
            Match fmMatch = FieldModifyPattern.Match(line);
            if (fmMatch.Success)
            {
                ProcessFieldModify(fmMatch);
                return;
            }
            Match hpMatch = HardpointSetPattern.Match(line);
            if (hpMatch.Success)
            {
                ProcessHardpointSet(hpMatch, weaponManager, aircraft);
                return;
            }
            Match tMatch = TransformPattern.Match(line);
            if (tMatch.Success)
            {
                ProcessTransform(tMatch, aircraft);
                return;
            }
            Match wMatch = WeaponPattern.Match(line);
            if (wMatch.Success)
            {
                ProcessWeapon(wMatch, weaponManager);
                return;
            }
        }
        private static void ProcessFieldModify(Match match)
        {
            string target = match.Groups["target"].Value;
            string componentName = match.Groups["component"].Value;
            string fieldName = match.Groups["field"].Value;
            string valStr = match.Groups["value"].Value;
            if (float.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
            {
                WeaponMount mount = FindWeaponPrefabReference(target);
                if (mount != null && mount.prefab != null)
                {
                    if (componentName.Equals("TailHook", StringComparison.OrdinalIgnoreCase))
                    {
                        TailHook hook = mount.prefab.GetComponentInChildren<TailHook>(true);
                        if (hook != null)
                        {
                            FieldInfo fi = typeof(TailHook).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                            if (fi != null)
                            {
                                fi.SetValue(hook, floatVal);
                            }
                        }
                    }
                }
            }
        }
        private static void ProcessHardpointSet(Match match, WeaponManager weaponManager, Aircraft aircraft)
        {
            string operation = match.Groups["operation"].Value;
            int setIndex = int.Parse(match.Groups["setIndex"].Value);
            if (operation == "add")
            {
                if (setIndex >= weaponManager.hardpointSets.Length)
                {
                    int oldLen = weaponManager.hardpointSets.Length;
                    Array.Resize(ref weaponManager.hardpointSets, setIndex + 1);
                    for (int i = oldLen; i < setIndex; i++)
                    {
                        weaponManager.hardpointSets[i] = new HardpointSet
                        {
                            name = "weaponstation" + i, 
                            hardpoints = new System.Collections.Generic.List<Hardpoint>(),
                            weaponOptions = new System.Collections.Generic.List<WeaponMount>(),
                            precludingHardpointSets = new System.Collections.Generic.List<byte>()
                        };
                    }
                }
                if (weaponManager.hardpointSets[setIndex] == null)
                {
                    weaponManager.hardpointSets[setIndex] = new HardpointSet
                    {
                        name = "Tail Mount",
                        hardpoints = new System.Collections.Generic.List<Hardpoint>(),
                        weaponOptions = new System.Collections.Generic.List<WeaponMount>(),
                        precludingHardpointSets = new System.Collections.Generic.List<byte>()
                    };
                }
            }
            else if (operation == "addhardpoint")
            {
                Hardpoint item = new Hardpoint
                {
                    transform = new GameObject("Hardpoint_" + Guid.NewGuid().ToString().Substring(0, 8)).transform,
                    part = null,
                    bayDoors = new BayDoor[0],
                    BuiltInWeapons = new Weapon[0],
                    BuiltInTurrets = new Turret[0]
                };
                Type pylonType = typeof(Hardpoint).GetNestedType("HardpointPylon", BindingFlags.NonPublic);
                if (pylonType != null)
                {
                    Array emptyPylons = Array.CreateInstance(pylonType, 0);
                    typeof(Hardpoint).GetField("pylonOptions", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(item, emptyPylons);
                }
                weaponManager.hardpointSets[setIndex].hardpoints.Add(item);
            }
            else if (operation == "name")
            {
                string name = match.Groups["name"].Value.Replace("\"", "");
                var hps = weaponManager.hardpointSets[setIndex];
                hps.name = name;
            }
            else if (operation == "modifyhardpoint")
            {
                int hpIndex = int.Parse(match.Groups["hpIndex"].Value);
                string transformPath = match.Groups["transformPath"].Value.Replace("\"", "");
                string partPath = match.Groups["partPath"].Value.Replace("\"", "");
                var hps = weaponManager.hardpointSets[setIndex];
                var hardpoint = hps.hardpoints[hpIndex];
                if (!string.IsNullOrEmpty(transformPath))
                {
                    GameObject tObj = PathLookupFind(transformPath, aircraft);
                    if (tObj != null) hardpoint.transform = tObj.transform;
                }
                if (!string.IsNullOrEmpty(partPath))
                {
                    UnitPart[] parts = aircraft.GetComponentsInChildren<UnitPart>(true);
                    string localPartPath = partPath;
                    int firstSlash = partPath.IndexOf('/');
                    if (firstSlash >= 0) localPartPath = partPath.Substring(firstSlash + 1);
                    UnitPart part = parts.FirstOrDefault(p => p.name.Replace("(Clone)", "").Trim().Equals(localPartPath, StringComparison.OrdinalIgnoreCase)
                                                            || GetFullPath(p.transform).EndsWith("/" + localPartPath)
                                                            || GetFullPath(p.transform).EndsWith(localPartPath));
                    if (part != null) hardpoint.part = part;
                }
                if (hardpoint.part == null)
                {
                    hardpoint.part = aircraft.GetComponent<UnitPart>() ?? aircraft.GetComponentInChildren<UnitPart>(true);
                }
            }
            else if (operation == "precludehardpoint")
            {
                int hpIndex = int.Parse(match.Groups["hpIndex"].Value);
                var hps = weaponManager.hardpointSets[setIndex];
                if (!hps.precludingHardpointSets.Contains((byte)hpIndex))
                {
                    hps.precludingHardpointSets.Add((byte)hpIndex);
                }
            }
        }
        private static void ProcessTransform(Match match, Aircraft aircraft)
        {
            string path = match.Groups["path"].Value;
            GameObject target = PathLookupFind(path, aircraft);
            if (target == null) return;
            float px = float.Parse(match.Groups["posx"].Value);
            float py = float.Parse(match.Groups["posy"].Value);
            float pz = float.Parse(match.Groups["posz"].Value);
            float rx = float.Parse(match.Groups["rotx"].Value);
            float ry = float.Parse(match.Groups["roty"].Value);
            float rz = float.Parse(match.Groups["rotz"].Value);
            float sx = float.Parse(match.Groups["scalex"].Value);
            float sy = float.Parse(match.Groups["scaley"].Value);
            float sz = float.Parse(match.Groups["scalez"].Value);
            target.transform.localPosition = new Vector3(px, py, pz);
            target.transform.localEulerAngles = new Vector3(rx, ry, rz);
            target.transform.localScale = new Vector3(sx, sy, sz);
        }
        private static WeaponMount FindWeaponPrefabReference(string prefabName)
        {
            if (string.Equals(prefabName, "null", StringComparison.OrdinalIgnoreCase)) return null;
            if (Encyclopedia.WeaponLookup != null && Encyclopedia.WeaponLookup.TryGetValue(prefabName, out WeaponMount encyMount) && encyMount != null)
            {
                return encyMount;
            }
            if (Encyclopedia.i != null && Encyclopedia.i.weaponMounts != null)
            {
                WeaponMount found = Encyclopedia.i.weaponMounts.FirstOrDefault(w => w != null && (w.name.Equals(prefabName, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(w.jsonKey) && w.jsonKey.Equals(prefabName, StringComparison.OrdinalIgnoreCase))));
                if (found != null) return found;
            }
            return Resources.Load<WeaponMount>(prefabName) 
                ?? Resources.FindObjectsOfTypeAll<WeaponMount>().FirstOrDefault(w => w.name.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
        }
        private static void ProcessWeapon(Match match, WeaponManager weaponManager)
        {
            string operation = match.Groups["operation"].Value;
            int setIndex = int.Parse(match.Groups["setIndex"].Value);
            string prefabName = match.Groups["prefabName"].Value;
            WeaponMount prefab = FindWeaponPrefabReference(prefabName);
            HardpointSet hps = weaponManager.hardpointSets[setIndex - 1];
            if (operation == "add")
            {
                if (prefab == null && prefabName.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    if (!hps.weaponOptions.Contains(null)) hps.weaponOptions.Add(null);
                }
                else if (prefab != null && !hps.weaponOptions.Contains(prefab))
                {
                    hps.weaponOptions.Add(prefab);
                }
                else if (prefab == null && !prefabName.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"[QolComboFix] Failed to resolve WeaponMount prefab '{prefabName}' for set {setIndex}");
                }
            }
        }
        private static GameObject PathLookupFind(string path, Aircraft aircraft)
        {
            if (aircraft == null) return null;
            string localPath = path;
            int firstSlash = path.IndexOf('/');
            if (firstSlash >= 0)
            {
                localPath = path.Substring(firstSlash + 1);
            }
            Transform[] transforms = aircraft.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                string fullPath = GetFullPath(t);
                if (fullPath.EndsWith("/" + localPath) || fullPath.EndsWith(localPath))
                {
                    return t.gameObject;
                }
            }
            return null;
        }
        private static string GetFullPath(Transform transform)
        {
            string text = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                text = transform.name + "/" + text;
            }
            return text;
        }
    }
    [HarmonyPatch(typeof(WeaponManager), "InitializeWeaponManager")]
    public static class WeaponManagerInitializePatch
    {
        [HarmonyPrefix]
        public static void Prefix(WeaponManager __instance)
        {
            if (__instance != null && __instance.hardpointSets != null)
            {
                FieldInfo aircraftField = typeof(WeaponManager).GetField("aircraft", BindingFlags.NonPublic | BindingFlags.Instance);
                if (aircraftField != null)
                {
                    Aircraft aircraft = (Aircraft)aircraftField.GetValue(__instance);
                    if (aircraft != null && aircraft.loadout != null && aircraft.loadout.weapons != null)
                    {
                        if (aircraft.loadout.weapons.Count < __instance.hardpointSets.Length)
                        {
                            int diff = __instance.hardpointSets.Length - aircraft.loadout.weapons.Count;
                            for (int i = 0; i < diff; i++)
                            {
                                aircraft.loadout.weapons.Add(null);
                            }
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(TailHook), "CheckDeployConditions")]
    public static class TailHookDeployConditionsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TailHook __instance, ref bool ___deployed, ref bool ___hooked, Aircraft ___aircraft)
        {
            if (!QolComboFixPlugin.Cfg_Extra_Tailhook_Enable.Value) return true;
            if (!QolComboFixPlugin.Cfg_Extra_Tailhook_Vanilla_Enable.Value) return true;
            if (___aircraft == null) return true;
            bool shouldDeploy = ___aircraft.gearDeployed;
            if (shouldDeploy && !___deployed)
            {
                if (___aircraft == SceneSingleton<CombatHUD>.i.aircraft)
                {
                    SceneSingleton<AircraftActionsReport>.i.ReportText("Tail Hook Deployed", 4f);
                }
                ___deployed = true;
                __instance.enabled = true;
            }
            else if (!shouldDeploy && ___deployed)
            {
                if (___aircraft == SceneSingleton<CombatHUD>.i.aircraft)
                {
                    SceneSingleton<AircraftActionsReport>.i.ReportText("Tail Hook Retracted", 4f);
                }
                ___deployed = false;
                if (___hooked)
                {
                    __instance.Unhook();
                }
            }
            return false; 
        }
    }
}