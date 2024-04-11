using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MazeGeneratorMod
{
    internal class FinishCelebrationHandler
    {
        public static bool celebrationActive = false;

        private static readonly List<Flare> flares = new List<Flare>();
        private static readonly List<FireExtinguisher> fireExtinguishers = new List<FireExtinguisher>();
        private static readonly List<BaseSpotLight> baseSpotLights = new List<BaseSpotLight>();
        private static readonly List<GameObject> posters = new List<GameObject>();

        private static readonly float lowerHeight = -1.6f;
        private static readonly float spotLightIntensity = 1.5f;

        private static readonly FieldInfo fxIsPlayingField = typeof(FireExtinguisher).GetField("fxIsPlaying", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _poweredField = typeof(BaseSpotLight).GetField("_powered", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo currentYawField = typeof(BaseSpotLight).GetField("currentYaw", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo targetYawField = typeof(BaseSpotLight).GetField("targetYaw", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo currentPitchField = typeof(BaseSpotLight).GetField("currentPitch", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo targetPitchField = typeof(BaseSpotLight).GetField("targetPitch", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FMOD.Studio.EventInstance finishSound = new FMOD.Studio.EventInstance();

        public static void SpawnCelebration(Vector3 finishLocationCenter)
        {
            if (celebrationActive) { return; }

            finishSound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            finishSound = Mod.CreateSound("event:/sub/rocket/stage_build", true);

            celebrationActive = true;

            float radius = 4f;
            int numItems = 8;

            for (int i = 0; i < numItems * 2; i++)
            {
                float degrees = (i / (numItems * 2f)) * 360f;
                float x = radius * (float)Math.Cos(degrees * (Math.PI / 180f));
                float z = radius * (float)Math.Sin(degrees * (Math.PI / 180f));
                Vector3 position = finishLocationCenter + new Vector3(x, lowerHeight, z);

                if (i % 2 == 0)
                {
                    float hue = i / (numItems * 2f);
                    Color color = Color.HSVToRGB(hue, 1f, 1f);
                    Quaternion rotation = Quaternion.LookRotation(
                        finishLocationCenter + new Vector3(0f, lowerHeight, 0f) - (finishLocationCenter + new Vector3(x, lowerHeight, z))
                    );

                    SpawnSpotLight(position, rotation, color);
                }
                else
                {
                    SpawnFireExtinguisher(position);
                }
            }

            float flareOffset = 1.5f;

            SpawnFlare(finishLocationCenter + new Vector3(-flareOffset, lowerHeight, flareOffset));
            SpawnFlare(finishLocationCenter + new Vector3(flareOffset, lowerHeight, flareOffset));
            SpawnFlare(finishLocationCenter + new Vector3(-flareOffset, lowerHeight, -flareOffset));
            SpawnFlare(finishLocationCenter + new Vector3(flareOffset, lowerHeight, -flareOffset));

            GameObject poster = MazeHandler.SpawnPoster(
                finishLocationCenter + new Vector3(-4.4171f, 0.5f, 0f),
                Quaternion.Euler(0f, 90f, 0f)
            );
            posters.Add(poster);

            GameObject poster2 = MazeHandler.SpawnPoster(
                finishLocationCenter + new Vector3(4.4171f, 0.5f, 0f),
                Quaternion.Euler(0f, -90f, 0f)
            );
            posters.Add(poster2);

            MesmerizedScreenFXController screenFX = MainCamera.camera.GetComponent<MesmerizedScreenFXController>();
            screenFX.StartHypnose();
        }

        public static void DestroyCelebration()
        {
            if (!celebrationActive) { return; }

            finishSound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);

            foreach (var item in flares) { UnityEngine.Object.Destroy(item.gameObject); }
            flares.Clear();

            foreach (var item in fireExtinguishers) { UnityEngine.Object.Destroy(item.gameObject); }
            fireExtinguishers.Clear();

            foreach (var item in baseSpotLights) { UnityEngine.Object.Destroy(item.gameObject); }
            baseSpotLights.Clear();

            foreach (var item in posters) { UnityEngine.Object.Destroy(item.gameObject); }
            posters.Clear();

            MesmerizedScreenFXController screenFX = MainCamera.camera.GetComponent<MesmerizedScreenFXController>();
            screenFX.StopHypnose();

            celebrationActive = false;
        }

        private static void SpawnSpotLight(Vector3 position, Quaternion rotation, Color color)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(Mod.cachedPrefabs[TechType.Spotlight]);
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;

            BaseSpotLight baseSpotLight = gameObject.GetComponent<BaseSpotLight>();

            Light light = baseSpotLight.light.GetComponent<Light>();
            light.color = color;
            light.intensity = spotLightIntensity;

            // Prevent deconstruction
            SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>();
            sphereCollider.enabled = false;

            baseSpotLights.Add(baseSpotLight);
        }

        private static void SpawnFlare(Vector3 position)
        {
            GameObject flareGameObject = UnityEngine.Object.Instantiate(Mod.cachedPrefabs[TechType.Flare]);
            flareGameObject.transform.position = position;

            // Make the flare invisisble
            MeshRenderer[] meshRenderers = flareGameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = false;
            }

            // Prevent picking up the flare
            CapsuleCollider capsuleCollider = flareGameObject.GetComponentInChildren<CapsuleCollider>();
            capsuleCollider.enabled = false;

            Flare flare = flareGameObject.GetComponent<Flare>();
            flare.fxControl.Play(1); // fire

            flares.Add(flare);
        }

        private static void SpawnFireExtinguisher(Vector3 position)
        {
            GameObject fireExtinguisherGameObject = UnityEngine.Object.Instantiate(Mod.cachedPrefabs[TechType.FireExtinguisher]);
            fireExtinguisherGameObject.transform.position = position;
            fireExtinguisherGameObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, -18f, 0f);
            
            FireExtinguisher fireExtinguisher = fireExtinguisherGameObject.GetComponent<FireExtinguisher>();

            fireExtinguishers.Add(fireExtinguisher);
        }

        [HarmonyPatch(typeof(FireExtinguisher))]
        [HarmonyPatch("Update")]
        internal class Patch_FireExtinguisher_Update
        {
            [HarmonyPrefix]
            public static bool Prefix(FireExtinguisher __instance)
            {
                if (fireExtinguishers.Contains(__instance))
                {
                    if (__instance.fxControl != null && !(bool)fxIsPlayingField.GetValue(__instance))
                    {
                        __instance.fxControl.Play(0);
                        fxIsPlayingField.SetValue(__instance, true);
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(BaseSpotLight))]
        [HarmonyPatch("Update")]
        internal class Patch_BaseSpotLight_Update
        {
            [HarmonyPrefix]
            public static bool Prefix(BaseSpotLight __instance)
            {
                if (baseSpotLights.Contains(__instance))
                {
                    __instance.CancelInvoke("UpdatePower");
                    __instance.CancelInvoke("UpdateTarget");

                    _poweredField.SetValue(__instance, true);

                    __instance.UpdateSweepAnimation();

                    float currentYaw = (float)currentYawField.GetValue(__instance);
                    float targetYaw = (float)targetYawField.GetValue(__instance);
                    float currentPitch = (float)currentPitchField.GetValue(__instance);
                    float targetPitch = (float)targetPitchField.GetValue(__instance);

                    currentYawField.SetValue(__instance, Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * 2f));
                    currentPitchField.SetValue(__instance, Mathf.LerpAngle(currentPitch, targetPitch, Time.deltaTime * 2f));
                    __instance.foundationPivot.localEulerAngles = new Vector3(0f, currentYaw, 0f);
                    __instance.lightPivot.localEulerAngles = new Vector3(currentPitch, 0f, 0f);

                    __instance.light.SetActive(true);
                    __instance.vfxSpotLight.SetLightActive(true);

                    return false;
                }

                return true;
            }
        }
    }
}
