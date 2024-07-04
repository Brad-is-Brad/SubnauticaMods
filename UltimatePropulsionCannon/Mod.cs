using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UWE;

namespace UltimatePropulsionCannon
{
    internal class Mod
    {
        static GameObject infoLabelGameObject = null;
        public static InfoLabel infoLabel = null;

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Awake")]
        public class Patch_Player_Awake : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                if (infoLabelGameObject == null)
                {
                    infoLabelGameObject = new GameObject("BradIsBrad.UltimatePropulsionCannon.InfoLabel");
                    infoLabel = infoLabelGameObject.AddComponent<InfoLabel>();
                    DontDestroyOnLoad(infoLabelGameObject);

                    new GameObject().AddComponent<ConsoleCommandListener>();
                }
            }
        }

        public static void LogDebug(string msg)
        {
            if (Plugin.config.debug)
            {
                Plugin.Logger.LogInfo(msg);
            }
        }
    }
}
