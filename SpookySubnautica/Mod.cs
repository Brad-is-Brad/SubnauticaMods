using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using UWE;
using FMOD.Studio;
using FMODUnity;
using FMOD;
using SpookySubnautica.Handlers;
using Story;

namespace SpookySubnautica
{
    class Mod
    {
        public static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static System.Random random = new System.Random();
        static GameObject infoLabelGameObject = null;
        public static InfoLabel infoLabel = null;

        public enum EffectType
        {
            None,
            GhostCrash,
            PDA,
            PinkScreen,
            Spider,
        }

        public static EffectType lastEffectType = EffectType.None;
        public static float lastEffectTime = 0;
        public static float minTimeBetweenEffects = 99999f;

        // TODO: menu screen can mess up sometimes (credits, quit game, etc.)

        public static readonly Dictionary<TechType, GameObject> cachedPrefabs = new Dictionary<TechType, GameObject>();
        static readonly List<TechType> relevantTechTypes = new List<TechType>()
        {
            TechType.Crash,
            TechType.GhostLeviathan,
            TechType.Shocker,
            TechType.PropulsionCannon,
            TechType.SpineEel,
            TechType.CaveCrawler,
        };

        static List<string> forbiddenStoryGoals = new List<string>()
        {
            "UnlockGlassDome",
            "UnlockLargeGlassDome",
            "Goal_Builder",
            "Goal_Seaglide",
            "Goal_Fins", // The fabricator
            "Goal_Knife",

            //"Goal_StasisRifle", // Craig McGill
            //"Goal_Lifepod2", The Aurora suffered a hull failure, 0 human lifesigns detected
        };

        [HarmonyPatch(typeof(DayNightCycle))]
        [HarmonyPatch("Update")]
        public class Patch_DayNightCycle_Update : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(DayNightCycle __instance)
            {
                try
                {
                    Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"Mod.Update Error: {e}");
                }

                try
                {
                    StormHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"StormHandler.Update Error: {e}");
                }

                try
                {
                    FlashlightHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"FlashlightHandler.Update Error: {e}");
                }

                try
                {
                    PDAHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"PDAHandler.Update Error: {e}");
                }

                try
                {
                    GhostCrashHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"GhostCrashHandler.Update Error: {e}");
                }

                try
                {
                    DayNightHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"DayNightHandler.Update Error: {e}");
                }

                try
                {
                    SpiderHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"SpiderHandler.Update Error: {e}");
                }

                try
                {
                    PinkScreenHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"PinkScreenHandler.Update Error: {e}");
                }

                try
                {
                    ColorHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"ColorHandler.Update Error: {e}");
                }

                try
                {
                    MetalThumpHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"MetalThumpHandler.Update Error: {e}");
                }

                try
                {
                    AnimalScreechHandler.Update();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"AnimalScreechHandler.Update Error: {e}");
                }
            }
        }

        [HarmonyPatch(typeof(StoryGoalScheduler))]
        [HarmonyPatch("Schedule")]
        public class Patch_StoryGoalScheduler_Schedule : MonoBehaviour
        {
            [HarmonyPrefix]
            public static bool Prefix(StoryGoalScheduler __instance, StoryGoal goal)
            {
                if (forbiddenStoryGoals.Contains(goal.key))
                {
                    Plugin.Logger.LogInfo($"{System.Math.Floor(Time.time)} - Preventing StoryGoal: {goal.key} - Delay: {goal.delay}");
                    return false;
                }

                Plugin.Logger.LogInfo($"{System.Math.Floor(Time.time)} - Allowing StoryGoal: {goal.key} - Delay: {goal.delay}");
                return true;
            }
        }

        public static void Update()
        {
            if (Player.main == null) { return; }

            if (infoLabelGameObject == null)
            {
                infoLabelGameObject = new GameObject("BradIsBradInfoLabel");
                infoLabel = infoLabelGameObject.AddComponent<InfoLabel>();
            }

            if (Input.GetKey(KeyCode.Q))
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    UpdateLatestEffect(EffectType.GhostCrash);
                    // TODO: broken
                    //GhostCrashHandler.StopEffect();
                    GhostCrashHandler.SpawnCrashFishGhost();
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    UpdateLatestEffect(EffectType.PDA);
                    PDAHandler.PlayRandomPDAMessage();
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    UpdateLatestEffect(EffectType.PinkScreen);
                    PinkScreenHandler.StopEffect();
                    PinkScreenHandler.StartEffect();
                }

                if (Input.GetKeyDown(KeyCode.I))
                {
                    UpdateLatestEffect(EffectType.Spider);
                    SpiderHandler.StopEffect();
                    SpiderHandler.StartEffect();
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    StormHandler.LightningStrike();
                }

                if (Input.GetKeyDown(KeyCode.U))
                {
                    MetalThumpHandler.StopEffect();
                    MetalThumpHandler.StartEffect();
                }

                if (Input.GetKeyDown(KeyCode.J))
                {
                    AnimalScreechHandler.StartEffect();
                }
            }
        }

        public static void UpdateLatestEffect(EffectType effectType)
        {
            lastEffectType = effectType;
            lastEffectTime = Time.time;
        }

        public static bool CanPlayEffect(EffectType effectType, bool canDoublePlay)
        {
            if (
                DayNightCycle.main.GetDay() > 0.85f // Don't start until the first night
                && (!lastEffectType.Equals(effectType) || canDoublePlay) // Don't double play effects unless the effect allows it
                && Time.time - lastEffectTime >= minTimeBetweenEffects // Don't play effects too close together
                && (
                    AtmosphereDirector.main.GetBiomeOverride() == null
                    || !AtmosphereDirector.main.GetBiomeOverride().StartsWith("Prison_")
                ) // Don't play effects in the precursor prison
            )
            {
                Plugin.Logger.LogInfo($"{System.Math.Floor(Time.time)} - Allowing Effect: {effectType}");
                return true;
            }

            return false;
        }

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Awake")]
        public class Patch_Player_Awake : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                new GameObject().AddComponent<ConsoleCommandListener>();
                StormHandler.Start();
                PDAHandler.Start();
            }
        }

        [HarmonyPatch(typeof(Creature))]
        [HarmonyPatch("Start")]
        public class Patch_Creature_Start : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(Creature __instance)
            {
                __instance.hearingSensitivity *= 4f;
                __instance.eyeFOV *= 3f;

                __instance.Aggression.Add(1f);
                __instance.Scared.Add(-1f);
                __instance.Tired.Add(-1f);
            }
        }

        public static void LoadPrefabs()
        {
            foreach (TechType techType in relevantTechTypes)
            {
                LoadTechTypePrefab(techType);
            }
        }

        private static void LoadTechTypePrefab(TechType techType)
        {
            IEnumerator spawnTechType = SpawnTechTypeAsync(techType);
            CoroutineHost.StartCoroutine(spawnTechType);
            return;
        }

        private static IEnumerator SpawnTechTypeAsync(TechType techType)
        {
            if (!cachedPrefabs.ContainsKey(techType))
            {
                CoroutineTask<GameObject> request = CraftData.GetPrefabForTechTypeAsync(techType);
                yield return request;
                GameObject result = request.GetResult();
                result.transform.parent = null;

                cachedPrefabs.Add(techType, result);
            }
        }

        public static Sound LoadSound(string fileName, MODE mode, string busPath)
        {
            string filePath = Path.Combine(ModPath, $"sounds\\{fileName}");
            RuntimeManager.CoreSystem.createSound(filePath, mode, out Sound sound);
            PlaySound(sound, busPath, out Channel channel);
            channel.setVolume(0);
            return sound;
        }

        public static void PlaySound(Sound sound, string busPath, out Channel channel)
        {
            channel = default;
            Bus bus = RuntimeManager.GetBus(busPath);
            if (bus.getChannelGroup(out ChannelGroup channelGroup) != RESULT.OK || !channelGroup.hasHandle())
            {
                bus.lockChannelGroup();
            }

            if (bus.getChannelGroup(out channelGroup) != RESULT.OK)
            {
                Plugin.Logger.LogInfo($"PlaySound unable to getChannelGroup");
            }

            if (channelGroup.getPaused(out bool paused) != RESULT.OK)
            {
                Plugin.Logger.LogInfo($"PlaySound unable to getPaused");
            }

            if (RuntimeManager.CoreSystem.playSound(sound, channelGroup, paused, out channel) != RESULT.OK)
            {
                Plugin.Logger.LogInfo($"PlaySound unable to playSound");
            }
        }

        // ****************************************
        // Print components
        // ****************************************

        public static void PrintComponents(GameObject target, string label, bool printParentComps, bool printComps, bool printChildComps)
        {
            Plugin.Logger.LogMessage("----------------------------------------");

            if (printParentComps)
            {
                Component[] comps = target.GetComponentsInParent<Component>();
                foreach (Component comp in comps)
                {
                    Plugin.Logger.LogMessage("----------");
                    Plugin.Logger.LogMessage(label + " parent comp: " + comp);
                    Plugin.Logger.LogMessage(label + " parent comp name: " + comp.name);
                    Plugin.Logger.LogMessage(label + " parent comp tag: " + comp.tag);
                }
                Plugin.Logger.LogMessage("==========");
            }

            if (printComps)
            {
                Component[] comps = target.GetComponents<Component>();
                foreach (Component comp in comps)
                {
                    Plugin.Logger.LogMessage("----------");
                    Plugin.Logger.LogMessage(label + " comp: " + comp);
                    Plugin.Logger.LogMessage(label + " comp name: " + comp.name);
                    Plugin.Logger.LogMessage(label + " comp tag: " + comp.tag);
                }
                Plugin.Logger.LogMessage("==========");
            }

            if (printChildComps)
            {
                Component[] comps = target.GetComponentsInChildren<Component>();
                foreach (Component comp in comps)
                {
                    Plugin.Logger.LogMessage("----------");
                    Plugin.Logger.LogMessage(label + " child comp: " + comp);
                    Plugin.Logger.LogMessage(label + " child comp name: " + comp.name);
                    Plugin.Logger.LogMessage(label + " child comp tag: " + comp.tag);
                }
                Plugin.Logger.LogMessage("==========");
            }
            Plugin.Logger.LogMessage("========================================");
        }
    }
}