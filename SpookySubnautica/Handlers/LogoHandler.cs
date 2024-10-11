using FMOD;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class LogoHandler
    {
        static GameObject logoObject = null;
        static float logoYPos = -2f;

        static bool cameraRotateStarted = false;
        static float cameraRotateStartTime;

        static bool startingTransformChecked = false;
        static Vector3 startingCameraPosition;
        static Quaternion startingCameraRotation;

        static float logoAppeared;
        static bool atmosphereChanged = false;
        static bool soundStarted = false;
        static Sound bassDropSound;

        static Sound ambianceSound;
        static Channel ambianceChannel;

        static Sound musicBoxSound;
        static Channel musicBoxChannel;

        static float soundVolume = 0.5f;

        static FieldInfo logoObjectField = typeof(MenuLogo).GetField("logoObject", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch(typeof(MenuLogo))]
        [HarmonyPatch("Start")]
        public class Patch_MenuLogo_Start : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(MenuLogo __instance)
            {
                Plugin.Logger.LogInfo($"MenuLogo.Start!");
                logoAppeared = Time.time;
                Mod.LoadPrefabs();

                bassDropSound = Mod.LoadSound("bass drop.ogg", MODE.DEFAULT, PDAHandler.pdaBus);
                ambianceSound = Mod.LoadSound("ambiance.ogg", MODE.DEFAULT, PDAHandler.pdaBus);
                musicBoxSound = Mod.LoadSound("music box.ogg", MODE.DEFAULT, PDAHandler.pdaBus);

                __instance.InvokeRepeating("OnLanguageChanged", 1.0f, 0.02f);
            }
        }

        [HarmonyPatch(typeof(MenuLogo))]
        [HarmonyPatch("OnLanguageChanged")]
        public class Patch_MenuLogo_OnLanguageChanged : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(MenuLogo __instance)
            {
                if (logoObject != null && logoObject != (GameObject)logoObjectField.GetValue(__instance))
                {
                    Destroy(__instance);
                    return;
                }

                if (logoObject == null)
                {
                    logoObject = (GameObject)logoObjectField.GetValue(__instance);
                }

                if (logoObject != null && atmosphereChanged)
                {
                    logoObject.transform.position = new Vector3(
                        logoObject.transform.position.x,
                        logoYPos + (float)Math.Sin(Time.time % (Math.PI * 2)) * 0.2f,
                        logoObject.transform.position.z
                    );
                }

                if (
                    !cameraRotateStarted
                    && (
                        (Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.P))
                        || Time.time - logoAppeared > 10
                    )
                )
                {
                    cameraRotateStarted = true;
                    cameraRotateStartTime = Time.time;
                }

                float slowness = 1.5f;
                if (cameraRotateStarted && (Time.time - cameraRotateStartTime) < Math.PI * 2f * slowness)
                {
                    if (!soundStarted)
                    {
                        soundStarted = true;
                        Mod.PlaySound(bassDropSound, PDAHandler.pdaBus, out Channel ch1);
                        ch1.setVolume(soundVolume);
                    }

                    Transform cameraTransform = Camera.allCameras[1].transform;
                    if (startingTransformChecked == false)
                    {
                        startingCameraPosition = new Vector3(
                            cameraTransform.position.x,
                            cameraTransform.position.y,
                            cameraTransform.position.z
                        );
                        startingCameraRotation = new Quaternion(
                            cameraTransform.rotation.x,
                            cameraTransform.rotation.y,
                            cameraTransform.rotation.z,
                            cameraTransform.rotation.w
                        );
                        startingTransformChecked = true;
                    }

                    float timePassed = (Time.time - cameraRotateStartTime) / slowness;
                    float progress = (float)Math.Sin(timePassed - (Math.PI / 2f)) / 2f + 0.5f;
                    float zRotation = -360f * (float)((Time.time - cameraRotateStartTime) / (Math.PI * 2f * slowness));

                    Camera.allCameras[1].transform.rotation = Quaternion.Euler(
                        startingCameraRotation.eulerAngles.x + (90f * progress),
                        startingCameraRotation.eulerAngles.y,
                        zRotation
                    );

                    Camera.allCameras[1].transform.position = startingCameraPosition + new Vector3(0, -20f * progress, 0);

                    if (zRotation <= -180f && !atmosphereChanged)
                    {
                        Mod.PlaySound(ambianceSound, PDAHandler.pdaBus, out ambianceChannel);
                        ambianceChannel.setVolume(soundVolume);

                        Mod.PlaySound(musicBoxSound, PDAHandler.pdaBus, out musicBoxChannel);
                        musicBoxChannel.setVolume(soundVolume);

                        atmosphereChanged = true;
                        AtmosphereHandler.SetAtmosphere(true);

                        // Turn the logo red
                        ColorHandler.TotallyRecolorObject(logoObject, Color.red);

                        // Rotate the logo
                        logoObject.transform.rotation = Quaternion.Euler(-128, 187, -7);

                        // Create some River Prowlers (SpineEel)
                        for (int i = 0; i < 30; i++)
                        {
                            GameObject gameObject2 = CraftData.InstantiateFromPrefab(
                                Mod.cachedPrefabs[TechType.SpineEel],
                                TechType.SpineEel
                            );
                            gameObject2.transform.position = new Vector3(
                                0 + UnityEngine.Random.Range(-5, 5),
                                5,
                                15 + UnityEngine.Random.Range(-5, 5)
                            );
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(uGUI_MainMenu))]
        [HarmonyPatch("LoadGameAsync")]
        public class Patch_uGUI_MainMenu_LoadGameAsync : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(MenuLogo __instance)
            {
                AtmosphereHandler.atmosphereEnabled = true;
            }
        }

        [HarmonyPatch(typeof(uGUI_MainMenu))]
        [HarmonyPatch("StartNewGame")]
        public class Patch_uGUI_MainMenu_StartNewGame : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(MenuLogo __instance)
            {
                AtmosphereHandler.atmosphereEnabled = true;
            }
        }
    }
}
