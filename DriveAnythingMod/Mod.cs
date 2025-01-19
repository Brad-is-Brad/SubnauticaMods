using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DriveAnythingMod
{
    internal class Mod
    {
        public static string ModPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        static GameObject infoLabelGameObject = null;
        public static InfoLabel infoLabel = null;

        public static System.Random random = new System.Random();

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Awake")]
        public class Patch_Player_Awake : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                if (infoLabelGameObject == null)
                {
                    infoLabelGameObject = new GameObject("BradIsBrad_DriveAnythingMod_InfoLabel");
                    infoLabel = infoLabelGameObject.AddComponent<InfoLabel>();
                    DontDestroyOnLoad(infoLabelGameObject);

                    new GameObject().AddComponent<ConsoleCommandListener>();
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

                try { DriveAnythingHandler.Update(); } catch (Exception e) { Plugin.Logger.LogInfo(e); }
            }
        }

        static void Update()
        {
            if (infoLabel != null)
            {
                if (Input.GetKeyDown(KeyCode.I) && Input.GetKey(KeyCode.Q))
                {
                    infoLabel.labelEnabled = !infoLabel.labelEnabled;
                }
            }

            if (Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.Q))
            {
                DriveAnythingHandler.BeginPilot();
            }
        }

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("FixedUpdate")]
        public class Patch_Player_FixedUpdate : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                DriveAnythingHandler.FixedUpdate();
            }
        }

        public static void LogDebug(string message)
        {
            if (Plugin.config.debug) Plugin.Logger.LogDebug(message);
        }

        public static void PrintComponentsAndChildren(GameObject target, string label)
        {
            PrintComponents(target, label, false, true, false);
            PrintChildren(target, label);
        }

        public static void PrintComponents(GameObject target, string label, bool printParentComps, bool printComps, bool printChildComps)
        {
            Plugin.Logger.LogMessage("----------------------------------------");

            if (printParentComps)
            {
                Component[] comps = target.GetComponentsInParent<Component>();
                foreach (Component comp in comps)
                {
                    Plugin.Logger.LogMessage(label + " parent comp: " + comp);
                }
                Plugin.Logger.LogMessage("==========");
            }

            if (printComps)
            {
                Component[] comps = target.GetComponents<Component>();
                foreach (Component comp in comps)
                {
                    Plugin.Logger.LogMessage(label + " comp: " + comp);
                }
                Plugin.Logger.LogMessage("==========");
            }

            if (printChildComps)
            {
                Component[] comps = target.GetComponentsInChildren<Component>();
                foreach (Component comp in comps)
                {
                    Plugin.Logger.LogMessage(label + " child comp: " + comp);
                }
                Plugin.Logger.LogMessage("==========");
            }
            Plugin.Logger.LogMessage("========================================");
        }

        public static void PrintChildren(GameObject gameObject, string parentage)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject childGameObject = gameObject.transform.GetChild(i).gameObject;
                string newParentage = $"{parentage} -> {childGameObject.name}";
                PrintComponents(childGameObject, newParentage, false, true, false);

                if (childGameObject.transform.childCount > 0)
                {
                    PrintChildren(childGameObject, newParentage);
                }
            }
        }
    }
}
