using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UWE;

namespace CompanionsMod
{
    internal class PrefabHandler
    {
        public static readonly Dictionary<TechType, GameObject> cachedPrefabs = new Dictionary<TechType, GameObject>();

        [HarmonyPatch(typeof(MenuLogo))]
        [HarmonyPatch("Start")]
        public class Patch_MenuLogo_Start : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(MenuLogo __instance)
            {
                LoadPrefabs();
            }
        }

        public static void LoadPrefabs()
        {
            Plugin.Logger.LogInfo($"Loading prefabs...");
            foreach (TechType techType in Enum.GetValues(typeof(TechType)))
            {
                LoadTechTypePrefab(techType);
            }
        }

        private static void LoadTechTypePrefab(TechType techType)
        {
            IEnumerator spawnTechType = SpawnTechTypeAsync(techType);
            CoroutineHost.StartCoroutine(spawnTechType);
        }

        private static IEnumerator SpawnTechTypeAsync(TechType techType)
        {
            if (!cachedPrefabs.ContainsKey(techType))
            {
                CoroutineTask<GameObject> request = CraftData.GetPrefabForTechTypeAsync(techType);
                yield return request;
                GameObject result = request.GetResult();
                result.transform.parent = null;

                cachedPrefabs.Add(techType, result);
            }
        }
    }
}
