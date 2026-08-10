using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using NuclearOption.SavedMission;
using NuclearOption.Networking;
namespace QolComboFix.Extra
{
    [HarmonyPatch(typeof(Spawner), "SpawnAircraft")]
    public static class AITailhookPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player player, GameObject prefab, Loadout loadout, Hangar spawningHangar)
        {
            if (!QolComboFixPlugin.Cfg_Extra_Tailhook_Enable.Value) return;
            if (player != null) return; 
            if (spawningHangar == null) return; 
            Ship ship = spawningHangar.GetComponentInParent<Ship>();
            if (ship == null) return;
            if (prefab == null || loadout == null || loadout.weapons == null) return;
            Aircraft prefabAircraft = prefab.GetComponent<Aircraft>();
            if (prefabAircraft == null || prefabAircraft.weaponManager == null) return;
            WeaponManager prefabWm = prefabAircraft.weaponManager;
            for (int i = 0; i < prefabWm.hardpointSets.Length; i++)
            {
                HardpointSet hs = prefabWm.hardpointSets[i];
                if (hs != null && hs.name != null && hs.name.IndexOf("Tail", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    while (loadout.weapons.Count <= i)
                    {
                        loadout.weapons.Add(null);
                    }
                    if (loadout.weapons[i] == null)
                    {
                        if (hs.weaponOptions != null)
                        {
                            WeaponMount hook = hs.weaponOptions.FirstOrDefault(w => w != null && w.name.IndexOf("TailHook", StringComparison.OrdinalIgnoreCase) >= 0);
                            if (hook != null)
                            {
                                loadout.weapons[i] = hook;
                                Debug.Log($"[QolComboFix] Automatically equipped {hook.name} on AI {prefab.name} spawning on carrier {ship.name}");
                            }
                        }
                    }
                }
            }
        }
    }
}