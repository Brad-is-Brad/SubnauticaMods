using FMOD;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class GhostCrashHandler
    {
        private static Material ghostLeviathanMaterial = null;
        static Crash ghostCrash = null;

        static float lastEventTime;
        static float timeBetweenEvents = 60 * 3;

        static float minDestroyDistance = 1f;

        static bool scarySoundLoaded = false;
        static Sound scarySound;
        static string scarySoundFilename = "scary ghost sound.ogg";
        static float scarySoundFadeInTime = 3f;
        static float scarySoundMaxVolume = 0.5f;
        static Channel scarySoundChannel;
        static string scarySoundBus = "bus:/master/SFX_for_pause/PDA_pause/all/SFX/creatures surface";

        static MethodInfo inflateMethod = typeof(Crash).GetMethod("Inflate", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Update()
        {
            if (ghostLeviathanMaterial == null)
            {
                GetGhostLeviathanMaterial();
            }

            if (!scarySoundLoaded)
            {
                scarySoundLoaded = true;
                scarySound = Mod.LoadSound(scarySoundFilename, MODE.DEFAULT, scarySoundBus);
            }

            if (
                Player.main.currentSub == null // Player must not be in a sub or base
                && Time.time - lastEventTime >= timeBetweenEvents // Enough time must have passed
                && Player.main.IsUnderwaterForSwimming() // Player must be in water
                && Camera.main.transform.position.y < -5 // Player must be under water
                && Mod.CanPlayEffect(Mod.EffectType.GhostCrash, false) // Timing and ordering system must allow the effect to play
            )
            {
                Mod.UpdateLatestEffect(Mod.EffectType.GhostCrash);
                SpawnCrashFishGhost();
            }

            if (ghostCrash != null)
            {
                scarySoundChannel.setVolume(
                    Math.Min(
                        scarySoundMaxVolume,
                        (Time.time - lastEventTime) / scarySoundFadeInTime * scarySoundMaxVolume
                    )
                );

                if (Vector3.Distance(ghostCrash.transform.position, Camera.main.transform.position) < minDestroyDistance) {
                    UnityEngine.Object.Destroy(ghostCrash.gameObject);
                    scarySoundChannel.stop();
                }
            }
        }

        private static void GetGhostLeviathanMaterial()
        {
            if (!Mod.cachedPrefabs.ContainsKey(TechType.GhostLeviathan)) return;

            GameObject gameObject = CraftData.InstantiateFromPrefab(
                Mod.cachedPrefabs[TechType.GhostLeviathan],
                TechType.GhostLeviathan
            );

            ghostLeviathanMaterial = gameObject.GetComponentInChildren<Renderer>().material;
            UnityEngine.Object.Destroy(gameObject);
        }

        public static void SpawnCrashFishGhost()
        {
            if (!Mod.cachedPrefabs.ContainsKey(TechType.Crash)) return;
            if (ghostCrash != null) { UnityEngine.Object.Destroy(ghostCrash); }

            lastEventTime = Time.time;

            GameObject gameObject = CraftData.InstantiateFromPrefab(
                Mod.cachedPrefabs[TechType.Crash],
                TechType.Crash
            );

            gameObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 20f;
            gameObject.transform.LookAt(Camera.main.transform.position);
            gameObject.SetActive(true);

            Crash crash = gameObject.GetComponent<Crash>();
            crash.lastTarget.SetTarget(Player.main.gameObject);
            crash.AttackLastTarget();
            inflateMethod.Invoke(crash, new object[] { });

            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            UnityEngine.Object.Destroy(rb);

            TurnIntoAGhost(gameObject);

            ghostCrash = crash;

            Mod.PlaySound(scarySound, scarySoundBus, out scarySoundChannel);
            scarySoundChannel.setVolume(0f);
        }

        [HarmonyPatch(typeof(Crash))]
        [HarmonyPatch("Detonate")]
        public class Patch_Crash_Detonate : MonoBehaviour
        {
            [HarmonyPrefix]
            public static bool Prefix(Crash __instance)
            {
                if (__instance == ghostCrash)
                {
                    StopEffect();
                    return false;
                }

                return true;
            }
        }

        public static void StopEffect()
        {
            UnityEngine.Object.Destroy(ghostCrash.gameObject);
            ghostCrash = null;

            scarySoundChannel.stop();
        }

        private static void TurnIntoAGhost(GameObject __instance)
        {
            Renderer[] renderers1 = __instance.gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers1)
            {
                if (renderer.name.Contains("_Eye"))
                {
                    renderer.material.SetColor("_GlowColor", Color.red);
                    continue;
                }

                if (ghostLeviathanMaterial != null)
                {
                    renderer.material = ghostLeviathanMaterial;
                    renderer.material.SetColor("_GlowColor", Color.red);
                }
            }
        }
    }
}
