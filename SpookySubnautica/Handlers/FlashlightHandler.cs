using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class FlashlightHandler
    {
        static FlashLight curFlashlight = null;

        static int flickerNum = 0;
        static int numFlickers = 8;
        static float minTimeBetweenFlickers = 3f;
        static float maxTimeBetweenFlickers = 7f;
        static float minFlickerDuration = 0.05f;
        static float maxFlickerDuration = 0.25f;
        static Color darkColor = new Color(0.5f, 0.5f, 0.5f);
        static Color brightColor = Color.white;

        static float nextFlickerTime = 0;

        static List<FlashLight> flashLights = new List<FlashLight>();

        public static void Update()
        {
            if (curFlashlight != null && Time.time >= nextFlickerTime)
            {
                if (flickerNum == 0)
                {
                    curFlashlight.flashLight.color = brightColor;
                    nextFlickerTime += UnityEngine.Random.Range(minTimeBetweenFlickers, maxTimeBetweenFlickers);
                    flickerNum++;
                }
                else if (flickerNum % 2 == 0)
                {
                    curFlashlight.flashLight.color = brightColor;
                    nextFlickerTime += UnityEngine.Random.Range(
                        minFlickerDuration,
                        minFlickerDuration + (maxFlickerDuration - minFlickerDuration) / 2f
                    );
                    flickerNum++;
                }
                else
                {
                    curFlashlight.flashLight.color = darkColor;
                    nextFlickerTime += UnityEngine.Random.Range(minFlickerDuration, maxFlickerDuration);
                    flickerNum++;
                }

                if (flickerNum > numFlickers) flickerNum = 0;
            }
        }

        [HarmonyPatch(typeof(PlayerTool))]
        [HarmonyPatch("OnDraw")]
        public class Patch_PlayerTool_OnDraw : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(PlayerTool __instance)
            {
                FlashLight drawnFlashLight = __instance.GetComponent<FlashLight>();
                if (drawnFlashLight != null && !flashLights.Contains(drawnFlashLight))
                {
                    flashLights.Add(drawnFlashLight);
                    curFlashlight = drawnFlashLight;
                    curFlashlight.flashLight.range *= 2f;
                    curFlashlight.flashLight.spotAngle /= 2f;

                    nextFlickerTime = Time.time + 10f;
                }
            }
        }
    }
}
