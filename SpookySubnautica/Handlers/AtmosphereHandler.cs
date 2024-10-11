using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class AtmosphereHandler
    {
        static WaterscapeVolume waterscapeVolume;

        static Color specularDark = new Color(0.39f, 0.39f, 0.39f, 1f);
        static Color specularLight = new Color(1f, 1f, 1f, 1f);

        public static bool atmosphereEnabled = false;

        [HarmonyPatch(typeof(WaterscapeVolume))]
        [HarmonyPatch("Awake")]
        public class Patch_WaterscapeVolume_Awake : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(WaterscapeVolume __instance)
            {
                waterscapeVolume = __instance;
                if (atmosphereEnabled)
                {
                    SetAtmosphere();
                }
            }
        }

        public static void SetAtmosphere(bool setTime = false)
        {
            waterscapeVolume.emissionAmbientScale = 16f;
            waterscapeVolume.scatteringPhase = -1f;
            waterscapeVolume.sunAttenuation = 0.5f;
            waterscapeVolume.sunLightAmount = 25f;
            waterscapeVolume.waterTransmission = 0.25f;
            waterscapeVolume.aboveWaterDensityScale = 1000f;

            waterscapeVolume.sky.StarIntensity = 5f;

            if (setTime)
            {
                // Only do this on the menu screen
                waterscapeVolume.sky.Timeline = 5.6f;
            }

            waterscapeVolume.sky.SunSize = 8f;
            waterscapeVolume.sky.Exposure = 0.2f;
            waterscapeVolume.sky.SkyTint = Color.red;
            waterscapeVolume.sky.sunColorMultiplier = 1f;

            waterscapeVolume.sky.secondaryLightColor = Color.red;

            waterscapeVolume.sky.planetAmbientLight = Color.red;
            waterscapeVolume.sky.planetInnerCorona = Color.red;
            waterscapeVolume.sky.planetOuterCorona = Color.red;
            waterscapeVolume.sky.planetRimColor = Color.red;

            waterscapeVolume.sky.planetRadius = waterscapeVolume.sky.planetRadius * 1.5f;

            waterscapeVolume.sky.SkyboxMaterial.color = Color.red;

            Color color = Color.red;
            float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            Color b = Color.Lerp(specularDark, specularLight, luminance);
            waterscapeVolume.sky.SkyboxMaterial.SetColor(ShaderPropertyID._Color, color);
            waterscapeVolume.sky.SkyboxMaterial.SetColor(ShaderPropertyID._SpecColor, Color.Lerp(color, b, 1f - 0.5f * luminance));
        }
    }
}
