using FMOD;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class SpiderHandler
    {
        static float enterBaseTime;
        static float timeBetweenEffects = 60 * 5;
        static float minTimeInBase = 10;
        static float effectStartTime;
        static float maxEffectTime = 10;

        static Sound pdaSound;
        static bool pdaSoundLoaded = false;
        static bool pdaSoundPlayed = false;
        static string pdaSoundFilename = "you should not have come to this place.ogg";

        static bool lastPowerEnabled = true;
        static bool powerEnabled = true;

        static Base lastBase = null;
        static float lastSpiderSpawn = Time.time;
        static float timeBetweenSpiderSpawns = .25f;

        static bool effectActive = false;
        static List<GameObject> spiders = new List<GameObject>();
        static Light light = null;
        static float lightIntensity = 2f;
        static float lightIntensityPerSecond = 0.5f;

        static Vector3 cellCenterPosition = Vector3.zero;

        static bool scarySoundLoaded = false;
        static Sound scarySound;
        static string scarySoundFilename = "scary spider sound.ogg";
        static float scarySoundFadeInTime = 10f;
        static float scarySoundMaxVolume = 0.2f;
        static Channel scarySoundChannel;
        static string scarySoundBus = "bus:/master/SFX_for_pause/PDA_pause/all/SFX/creatures surface";

        public static void Update()
        {
            if (!pdaSoundLoaded)
            {
                pdaSoundLoaded = true;
                pdaSound = Mod.LoadSound(pdaSoundFilename, MODE.DEFAULT, PDAHandler.pdaBus);
            }

            if (!scarySoundLoaded)
            {
                scarySoundLoaded = true;
                scarySound = Mod.LoadSound(scarySoundFilename, MODE.DEFAULT, PDAHandler.pdaBus);
            }

            if (Player.main && Player.main.currentSub && !effectActive)
            {
                Base curBase = Player.main.currentSub.GetComponent<Base>();
                if (curBase != null)
                {
                    if (lastBase != curBase)
                    {
                        enterBaseTime = Time.time;
                    }

                    lastBase = curBase;

                    if (
                        Time.time - effectStartTime >= timeBetweenEffects
                        && Time.time - enterBaseTime >= minTimeInBase
                        && Mod.CanPlayEffect(Mod.EffectType.Spider, false)
                    )
                    {
                        Mod.UpdateLatestEffect(Mod.EffectType.Spider);
                        StartEffect();
                    }
                }
            }

            SpawnSpiders();

            if (effectActive && Time.time - effectStartTime >= maxEffectTime)
            {
                StopEffect();
            }
        }

        public static void StartEffect()
        {
            if (lastBase == null) { return; }

            effectActive = true;
            effectStartTime = Time.time;
            pdaSoundPlayed = false;

            powerEnabled = false;

            try
            {
                Int3 curCell = lastBase.WorldToGrid(Camera.main.transform.position);

                Int3 normalizedCell = lastBase.NormalizeCell(curCell);
                Base.CellType normalizedCellType = lastBase.GetCellType(normalizedCell);

                Int3 cellSize = Base.CellSize[(uint)normalizedCellType];
                Vector3 cellPos = lastBase.GridToWorld(normalizedCell);
                Vector3 otherSideCellPos = lastBase.GridToWorld(normalizedCell + new Int3(cellSize.x - 1, 0, cellSize.z - 1));
                Vector3 cellCenterOffset = (otherSideCellPos - cellPos) / 2f;
                cellCenterPosition = cellPos + cellCenterOffset + new Vector3(0, -.5f, 0);

                light = new GameObject().AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.black;
                light.transform.position = cellCenterPosition + new Vector3(0, .5f, 0);
                light.intensity = lightIntensity;

                Mod.PlaySound(scarySound, scarySoundBus, out scarySoundChannel);
                scarySoundChannel.setVolume(0f);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogInfo($"{Time.time} - Exception trying to spawn spiders, stopping effect.");
                StopEffect();
            }
        }

        static void SpawnSpiders()
        {
            if (!effectActive) { return; }

            light.color = Color.red * ((Time.time - effectStartTime) / (maxEffectTime / 2f));

            if (!pdaSoundPlayed && (Time.time - effectStartTime > maxEffectTime * .6f))
            {
                PDAHandler.PlayPDASound(pdaSound);
                pdaSoundPlayed = true;
            }

            if (Time.time - lastSpiderSpawn > timeBetweenSpiderSpawns)
            {
                lastSpiderSpawn = Time.time;

                GameObject gameObject = CraftData.InstantiateFromPrefab(
                    Mod.cachedPrefabs[TechType.CaveCrawler],
                    TechType.CaveCrawler
                );
                gameObject.transform.position = cellCenterPosition + new Vector3(
                    UnityEngine.Random.Range(-.25f, .25f),
                    0f,
                    UnityEngine.Random.Range(-.25f, .25f)
                );
                spiders.Add(gameObject);

                Creature creature = gameObject.GetComponent<Creature>();
                creature.Aggression.Value = 1f;
                creature.Scared.Value = 0f;

                try
                {
                    // Efficient? No. Functional? Yes.
                    Collider[] spiderColliders = gameObject.GetComponentsInChildren<Collider>();
                    Collider[] playerColliders = Player.main.gameObject.GetComponentsInChildren<Collider>();

                    for (int i = 0; i < spiderColliders.Length; i++)
                    {
                        for (int j = 0; j < playerColliders.Length; j++)
                        {
                            Physics.IgnoreCollision(
                                spiderColliders[i],
                                playerColliders[j]
                            );
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"Error ignoring spider collisions: {e}");
                }
            }

            scarySoundChannel.setVolume(
                Math.Min(
                    scarySoundMaxVolume,
                    (Time.time - effectStartTime) / scarySoundFadeInTime * scarySoundMaxVolume
                )
            );
        }

        public static void StopEffect()
        {
            if (!effectActive) { return; }

            effectActive = false;

            if (light != null)
            {
                UnityEngine.Object.Destroy(light.gameObject);
            }
            
            foreach (GameObject spider in spiders)
            {
                UnityEngine.Object.Destroy(spider);
            }

            spiders.Clear();

            powerEnabled = true;

            scarySoundChannel.stop();
        }

        [HarmonyPatch(typeof(PowerRelay))]
        [HarmonyPatch("UpdatePowerState")]
        internal class Patch_PowerRelay_UpdatePowerState
        {
            [HarmonyPrefix]
            public static bool Prefix(PowerRelay __instance)
            {
                Base powerRelayBase = __instance.GetComponent<Base>();
                if (powerRelayBase != null && powerRelayBase == lastBase)
                {
                    UpdatePowerRelay(__instance);
                    return false;
                }

                return true;
            }
        }

        public static void UpdatePowerRelay(PowerRelay powerRelay)
        {
            FieldInfo isPoweredField = typeof(PowerRelay).GetField("isPowered", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo powerStatusField = typeof(PowerRelay).GetField("powerStatus", BindingFlags.NonPublic | BindingFlags.Instance);

            if (powerEnabled)
            {
                isPoweredField.SetValue(powerRelay, true);
                powerStatusField.SetValue(powerRelay, PowerSystem.Status.Normal);
            }
            else
            {
                isPoweredField.SetValue(powerRelay, false);
                powerStatusField.SetValue(powerRelay, PowerSystem.Status.Offline);
            }

            if (lastPowerEnabled != powerEnabled)
            {
                if (powerEnabled)
                {
                    powerRelay.powerUpEvent.Trigger(powerRelay);
                }
                else
                {
                    powerRelay.powerDownEvent.Trigger(powerRelay);
                }

                lastPowerEnabled = powerEnabled;
            }
        }
    }
}
