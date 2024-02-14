using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace SeaLevelMod
{
    class Mod
    {
        public static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        static Ocean ocean;
        static WaterPlane waterPlane;
        static readonly List<WaterSurface> waterSurfaces = new List<WaterSurface>();
        static float previousSeaLevel = 0f;

        public static void UpdateSeaLevel()
        {
            float seaLevelChange = Plugin.config.seaLevel - previousSeaLevel;

            if (ocean)
                ocean.transform.position = new Vector3(
                    ocean.transform.position.x,
                    ocean.transform.position.y + seaLevelChange,
                    ocean.transform.position.z
                );

            if (waterPlane)
                waterPlane.transform.position = new Vector3(
                    waterPlane.transform.position.x,
                    waterPlane.transform.position.y + seaLevelChange,
                    waterPlane.transform.position.z
                );

            foreach (WaterSurface waterSurface in waterSurfaces)
            {
                if (waterSurface)
                    waterSurface.transform.position = new Vector3(
                        waterSurface.transform.position.x,
                        waterSurface.transform.position.y + seaLevelChange,
                        waterSurface.transform.position.z
                    );
            }

            previousSeaLevel = Plugin.config.seaLevel;
        }

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Awake")]
        public class LoadMod : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Load(Player __instance)
            {
                new GameObject().AddComponent<ConsoleCommandListener>();
            }
        }

        public static bool hasTestedHab = false;

        [HarmonyPatch(typeof(DayNightCycle))]
        [HarmonyPatch("Update")]
        internal class Patch_DayNightCycle_Update
        {
            [HarmonyPostfix]
            public static void Postfix(DayNightCycle __instance)
            {
                // Avoid using too much CPU unnecessarily
                if (Plugin.config.seaLevelChangeSpeed == 0 && !Plugin.config.hasTide)
                    return;

                if (Plugin.config.hasTide)
                {
                    float tideChange = Math.Abs(Plugin.config.highTide - Plugin.config.lowTide);
                    Plugin.config.seaLevel = (float)(
                        (
                            Math.Sin((__instance.GetDayScalar() * 1000f) / (Math.PI * (100 * Plugin.config.tidePeriod)))
                            * (tideChange / 2f)
                        )
                        + ((tideChange / 2f) + Plugin.config.lowTide)
                    );
                }
                else
                {
                    float waterLevelChange = Plugin.config.seaLevelChangeSpeed * __instance.deltaTime;
                    Plugin.config.seaLevel += waterLevelChange;
                }

                UpdateSeaLevel();
            }
        }

        [HarmonyPatch(typeof(Ocean))]
        [HarmonyPatch("Awake")]
        internal class Patch_Ocean_Awake
        {
            [HarmonyPostfix]
            public static void Postfix(Ocean __instance)
            {
                ocean = __instance;
                ocean.transform.position = new Vector3(
                    ocean.transform.position.x,
                    ocean.transform.position.y + Plugin.config.seaLevel,
                    ocean.transform.position.z
                );
            }
        }

        [HarmonyPatch(typeof(WaterPlane))]
        [HarmonyPatch("Start")]
        internal class Patch_WaterPlane_Start
        {
            [HarmonyPostfix]
            public static void Postfix(WaterPlane __instance)
            {
                waterPlane = __instance;
                waterPlane.transform.position = new Vector3(
                    waterPlane.transform.position.x,
                    waterPlane.transform.position.y + Plugin.config.seaLevel,
                    waterPlane.transform.position.z
                );
            }
        }

        [HarmonyPatch(typeof(WaterSurface))]
        [HarmonyPatch("Start")]
        internal class Patch_WaterSurface_Start
        {
            [HarmonyPostfix]
            public static void Postfix(WaterSurface __instance)
            {
                if (!waterSurfaces.Contains(__instance))
                {
                    waterSurfaces.Add(__instance);

                    __instance.transform.position = new Vector3(
                        __instance.transform.position.x,
                        __instance.transform.position.y + Plugin.config.seaLevel,
                        __instance.transform.position.z
                    );
                }
            }
        }

        [HarmonyPatch(typeof(WorldForces))]
        [HarmonyPatch("DoFixedUpdate")]
        internal class Patch_WorldForces_Awake
        {
            [HarmonyPostfix]
            public static void Postfix(WorldForces __instance)
            {
                __instance.waterDepth = Plugin.config.seaLevel;
            }
        }
    }
}