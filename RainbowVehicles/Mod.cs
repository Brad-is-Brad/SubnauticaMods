using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RainbowVehicles
{
    public class Mod
    {
        public static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        enum SeamothColors
        {
            Main = 0,
            Name = 1,
            Interior = 2,
            Stripe1 = 3,
            Stripe2 = 4,
        }

        enum CyclopsColors
        {
            Base = 0,
            Stripe1 = 1,
            Stripe2 = 2,
            Name = 3,
        }

        enum PrawnSuitColors
        {
            Base = 0,
            Name = 1,
            Interior = 2,
            Stripe1 = 3,
            Stripe2 = 4,
        }

        static List<SubName> seamothSubNames = new List<SubName>();
        static List<SubName> cyclopsSubNames = new List<SubName>();
        static List<SubName> prawnSuitSubNames = new List<SubName>();

        [HarmonyPatch(typeof(DayNightCycle))]
        [HarmonyPatch("Update")]
        internal class Patch_DayNightCycle_Update
        {
            [HarmonyPostfix]
            public static void Postfix(DayNightCycle __instance)
            {
                UpdateSeaMothColors(__instance);
                UpdateCyclopsColors(__instance);
                UpdatePrawnSuitColors(__instance);
            }
        }

        [HarmonyPatch(typeof(SeaMoth))]
        [HarmonyPatch("Start")]
        internal class Patch_SeaMoth_Start
        {
            [HarmonyPrefix]
            public static void Prefix(SeaMoth __instance)
            {
                SubName subName = __instance.GetComponent<SubName>();
                if (subName != null && !seamothSubNames.Contains(subName))
                {
                    seamothSubNames.Add(subName);
                }
            }
        }

        [HarmonyPatch(typeof(SubRoot))]
        [HarmonyPatch("Start")]
        internal class Patch_SubRoot_Start
        {
            [HarmonyPrefix]
            public static void Prefix(SubRoot __instance)
            {
                if (!__instance.isCyclops) return;

                SubName cyclopsSubName = __instance.GetComponentInChildren<SubName>(includeInactive: true);

                if (cyclopsSubName != null && !cyclopsSubNames.Contains(cyclopsSubName))
                {
                    cyclopsSubNames.Add(cyclopsSubName);
                }
            }
        }

        [HarmonyPatch(typeof(Exosuit))]
        [HarmonyPatch("Start")]
        internal class Patch_Exosuit_Start
        {
            [HarmonyPrefix]
            public static void Prefix(Exosuit __instance)
            {
                SubName subName = __instance.GetComponent<SubName>();
                if (subName != null && !prawnSuitSubNames.Contains(subName))
                {
                    prawnSuitSubNames.Add(subName);
                }
            }
        }

        private static void UpdateSeaMothColors(DayNightCycle dayNightCycle)
        {
            seamothSubNames.RemoveAll(item => item == null);

            foreach (SubName seamothSubName in seamothSubNames)
            {
                Vector3[] cols = seamothSubName.GetColors();

                float inc = 1f / cols.Length;
                for (int i = 0; i < cols.Length; i++)
                {
                    if (i == (int)SeamothColors.Main && !Plugin.config.changeSeamothMain) continue;
                    if (i == (int)SeamothColors.Name && !Plugin.config.changeSeamothName) continue;
                    if (i == (int)SeamothColors.Interior && !Plugin.config.changeSeamothInterior) continue;
                    if (i == (int)SeamothColors.Stripe1 && !Plugin.config.changeSeamothStripe1) continue;
                    if (i == (int)SeamothColors.Stripe2 && !Plugin.config.changeSeamothStripe2) continue;

                    float hueValue =
                        (dayNightCycle.GetDayScalar() * Plugin.config.seamothChangeSpeed
                        + inc * i)
                        % 1f
                    ;

                    seamothSubName.SetColor(
                        i,
                        Vector3.one,
                        Color.HSVToRGB(hueValue, 1f, 1f)
                    );
                }
            }
        }

        private static void UpdateCyclopsColors(DayNightCycle dayNightCycle)
        {
            cyclopsSubNames.RemoveAll(item => item == null);

            foreach (SubName cyclopsSubName in cyclopsSubNames)
            {
                Vector3[] cols = cyclopsSubName.GetColors();
                
                float inc = 1f / cols.Length;
                for (int i = 0; i < cols.Length; i++)
                {
                    if (i == (int)CyclopsColors.Base && !Plugin.config.changeCyclopsBase) continue;
                    if (i == (int)CyclopsColors.Stripe1 && !Plugin.config.changeCyclopsStripe1) continue;
                    if (i == (int)CyclopsColors.Stripe2 && !Plugin.config.changeCyclopsStripe2) continue;
                    if (i == (int)CyclopsColors.Name && !Plugin.config.changeCyclopsName) continue;

                    float hueValue =
                        (dayNightCycle.GetDayScalar() * Plugin.config.cyclopsChangeSpeed
                        + inc * i)
                        % 1f
                    ;

                    cyclopsSubName.SetColor(
                        i,
                        Vector3.one,
                        Color.HSVToRGB(hueValue, 1f, 1f)
                    );
                }
            }
        }

        private static void UpdatePrawnSuitColors(DayNightCycle dayNightCycle)
        {
            prawnSuitSubNames.RemoveAll(item => item == null);

            foreach (SubName subName in prawnSuitSubNames)
            {
                Vector3[] cols = subName.GetColors();

                float inc = 1f / cols.Length;
                for (int i = 0; i < cols.Length; i++)
                {
                    if (i == (int)PrawnSuitColors.Base && !Plugin.config.changePrawnSuitBase) continue;
                    if (i == (int)PrawnSuitColors.Name && !Plugin.config.changePrawnSuitName) continue;
                    if (i == (int)PrawnSuitColors.Interior && !Plugin.config.changePrawnSuitInterior) continue;
                    if (i == (int)PrawnSuitColors.Stripe1 && !Plugin.config.changePrawnSuitStripe1) continue;
                    if (i == (int)PrawnSuitColors.Stripe2 && !Plugin.config.changePrawnSuitStripe2) continue;

                    float hueValue =
                        (dayNightCycle.GetDayScalar() * Plugin.config.prawnSuitChangeSpeed
                        + inc * i)
                        % 1f
                    ;

                    subName.SetColor(
                        i,
                        Vector3.one,
                        Color.HSVToRGB(hueValue, 1f, 1f)
                    );
                }
            }
        }
    }
}
