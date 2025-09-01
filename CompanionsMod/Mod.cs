using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CompanionsMod
{
    internal class Mod
    {
        public static string ModPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        static GameObject infoLabelGameObject = null;
        public static InfoLabel infoLabel = null;

        public static System.Random random = new System.Random();

        static bool needLoad = false;

        [HarmonyPatch(typeof(MenuLogo))]
        [HarmonyPatch("Start")]
        public class Patch_MenuLogo_Start : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(MenuLogo __instance)
            {
                PrefabHandler.LoadPrefabs();
            }
        }

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Awake")]
        public class Patch_Player_Awake : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                if (infoLabelGameObject == null)
                {
                    infoLabelGameObject = new GameObject("BradIsBrad_CompanionsMod_InfoLabel");
                    infoLabel = infoLabelGameObject.AddComponent<InfoLabel>();
                    DontDestroyOnLoad(infoLabelGameObject);

                    needLoad = true;
                    Plugin.Logger.LogInfo($"Player - Awake");
                }
            }
        }

        [HarmonyPatch(typeof(DayNightCycle))]
        [HarmonyPatch("Update")]
        internal class Patch_DayNightCycle_Update
        {
            [HarmonyPostfix]
            public static void Postfix(DayNightCycle __instance)
            {
                try { Update(); } catch (Exception e) { Plugin.Logger.LogInfo(e); }
            }
        }

        static void Update()
        {
            if (
                needLoad
                && uGUI_CompanionTab.instance != null
                && CompanionHandler.instance == null
                && WaitScreen.main.items.Count == 0 // Wait for game to finish loading
            )
            {
                CompanionHandler.instance = new CompanionHandler();
                SaveManager.Load();
                needLoad = false;
            }

            if (Input.GetKey(KeyCode.Q) && Input.GetKeyDown(KeyCode.I))
            {
                infoLabel.labelEnabled = !infoLabel.labelEnabled;
            }

            if (CompanionHandler.instance != null)
            {
                CompanionHandler.instance.DoUpdate();
            }

            if (uGUI_CompanionTab.instance == null)
            {
                uGUI_CompanionTab.Setup();
                Plugin.Logger.LogInfo($"uGUI_CompanionTab.instance: {uGUI_CompanionTab.instance}");
            }
            else
            {
                uGUI_CompanionTab.instance.DoUpdate();
            }
        }

        static List<string> forbiddenGameObjects = new List<string> {
            "Player",
        };

        public static GameObject GetLookingAtCreature()
        {
            try
            {
                Vector3 position = Camera.main.transform.position;
                int num = UWE.Utils.SpherecastIntoSharedBuffer(
                    position,
                    1.0f, //spherecastRadius
                    Camera.main.transform.forward,
                    200f
                );

                for (int i = 0; i < num; i++)
                {
                    RaycastHit raycastHit = UWE.Utils.sharedHitBuffer[i];
                    GameObject hitGameObject = raycastHit.collider.gameObject;

                    if (hitGameObject.GetComponent<Creature>())
                    {
                        bool isForbidden = false;
                        foreach (string forbiddenGameObject in forbiddenGameObjects)
                        {
                            if (hitGameObject.name.Contains(forbiddenGameObject))
                            {
                                isForbidden = true;
                                break;
                            }
                        }
                        if (isForbidden) continue;

                        return hitGameObject;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"{e}");
            }

            return null;
        }
    }
}
