using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RainbowSeamoth
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

        static List<SubName> seaMothSubNames = new List<SubName>();

        [HarmonyPatch(typeof(DayNightCycle))]
        [HarmonyPatch("Update")]
        internal class Patch_DayNightCycle_Update
        {
            [HarmonyPostfix]
            public static void Postfix(DayNightCycle __instance)
            {
                UpdateSeaMothColors(__instance);
            }
        }

        [HarmonyPatch(typeof(SeaMoth))]
        [HarmonyPatch("Start")]
        internal class Patch_SeaMoth_Start
        {
            [HarmonyPrefix]
            public static void Prefix(SeaMoth __instance)
            {
                SubName seaMothSubName = __instance.GetComponent<SubName>();
                if (seaMothSubName != null && !seaMothSubNames.Contains(seaMothSubName))
                {
                    seaMothSubNames.Add(seaMothSubName);
                }
            }
        }

        private static void UpdateSeaMothColors(DayNightCycle dayNightCycle)
        {
            seaMothSubNames.RemoveAll(item => item == null);

            foreach (SubName seaMothSubName in seaMothSubNames)
            {
                Vector3[] cols = seaMothSubName.GetColors();

                float inc = 1f / cols.Length;
                for (int i = 0; i < cols.Length; i++)
                {
                    if (i == (int)SeamothColors.Main && !Plugin.config.changeMain) continue;
                    if (i == (int)SeamothColors.Name && !Plugin.config.changeName) continue;
                    if (i == (int)SeamothColors.Interior && !Plugin.config.changeInterior) continue;
                    if (i == (int)SeamothColors.Stripe1 && !Plugin.config.changeStripe1) continue;
                    if (i == (int)SeamothColors.Stripe2 && !Plugin.config.changeStripe2) continue;

                    float hueValue =
                        (dayNightCycle.GetDayScalar() * Plugin.config.changeSpeed
                        + inc * i)
                        % 1f
                    ;

                    seaMothSubName.SetColor(
                        i,
                        Vector3.one,
                        Color.HSVToRGB(hueValue, 1f, 1f)
                    );
                }
            }
        }
    }
}
