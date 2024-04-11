using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MazeGeneratorMod
{
    internal class FloodHandler
    {
        public static bool floodingEnabled = false;
        private static float curFloodLevel = 0f;
        private static readonly float floodSpeed = 0.1f;

        [HarmonyPatch(typeof(Base))]
        [HarmonyPatch("GetHullStrength")]
        internal class Patch_Base_GetHullStrength
        {
            [HarmonyPrefix]
            public static bool Prefix(Base __instance, float __result)
            {
                if (Mod.mazeBase != null && __instance == Mod.mazeBase)
                {
                    __result = 1f;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(BaseFloodSim))]
        [HarmonyPatch("UpdateLeakers")]
        internal class Patch_Leakable_SpringNearestLeak
        {
            [HarmonyPrefix]
            public static bool Prefix(BaseFloodSim __instance)
            {
                Base curBase = __instance.GetComponentInParent<Base>();

                if (Mod.mazeBase != null && curBase == Mod.mazeBase)
                {
                    return false;
                }

                return true;
            }
        }

        public static void ToggleFloodBase()
        {
            if (Mod.mazeBase == null) { return; }

            floodingEnabled = !floodingEnabled;
        }

        public static void UpdateFloodLevel()
        {
            if (Mod.mazeBase == null) { return; }

            if (floodingEnabled)
            {
                curFloodLevel = Math.Min(1f, curFloodLevel + (floodSpeed * Time.deltaTime));
            }
            else
            {
                curFloodLevel = Math.Max(0f, curFloodLevel - (floodSpeed * Time.deltaTime));
            }

            BaseFloodSim baseFloodSim = Mod.mazeBase.GetComponent<BaseFloodSim>();
            if (baseFloodSim == null) { return; }

            // private Grid3Shape shape;
            FieldInfo shapeField = typeof(BaseFloodSim).GetField("shape", BindingFlags.Instance | BindingFlags.NonPublic);
            int size = ((Grid3Shape)shapeField.GetValue(baseFloodSim)).Size;

            // private float[] cellWaterLevel;
            FieldInfo cellWaterLevelField = typeof(BaseFloodSim).GetField("cellWaterLevel", BindingFlags.Instance | BindingFlags.NonPublic);
            float[] cellWaterLevel = (float[])cellWaterLevelField.GetValue(baseFloodSim);

            for (int i = 1; i < size - 1; i++)
            {
                if ((Mod.mazeBase.flowData[i] & 0x40) > 0)
                {
                    cellWaterLevel[i] = curFloodLevel;
                }
            }
        }
    }
}
