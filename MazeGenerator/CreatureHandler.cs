using FMOD.Studio;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MazeGeneratorMod
{
    internal class CreatureHandler
    {
        private static readonly List<Vector3> creatureSpawnPositions = new List<Vector3>();
        public static readonly List<GameObject> crashHomes = new List<GameObject>();
        private static readonly List<GameObject> crashes = new List<GameObject>();
        private static FieldInfo crashField = typeof(CrashHome).GetField("crash", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly float attackDistance = 5f;

        public static bool creaturesEnabled = false;

        public static void ToggleCreatures()
        {
            creaturesEnabled = !creaturesEnabled;
        }

        public static GameObject SpawnCreature(TechType techType, Vector3 position)
        {
            GameObject gameObject = CraftData.InstantiateFromPrefab(
                Mod.cachedPrefabs[techType],
                techType
            );
            gameObject.transform.position = position;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.parent = Mod.mazeBase.transform;

            gameObject.SetActive(true);

            // This should get rid of the sulphur spawning. The code below this that
            //  removed CrashPowder objects betrays my confidence in this.
            PrefabPlaceholdersGroup sulphurSpawner = gameObject.GetComponentInChildren<PrefabPlaceholdersGroup>();
            if (sulphurSpawner != null)
            {
                UnityEngine.Object.Destroy(sulphurSpawner);
            }

            crashHomes.Add(gameObject);
            creatureSpawnPositions.Add(position);

            return gameObject;
        }

        public static void DestroyCreatures()
        {
            foreach (GameObject crashHome in crashHomes)
            {
                Collider[] hitColliders = Physics.OverlapSphere(crashHome.transform.position, 0.1f);
                foreach (var hitCollider in hitColliders)
                {
                    Pickupable pickupable = hitCollider.GetComponent<Pickupable>();
                    if (pickupable != null && pickupable.name.Contains("CrashPowder"))
                    {
                        // Destroys the sulphur
                        UnityEngine.Object.Destroy(pickupable.gameObject);
                    }
                }

                // Destroys fish and home
                UnityEngine.Object.Destroy(crashHome);
            }

            creatureSpawnPositions.Clear();
            crashes.Clear();
            crashHomes.Clear();
        }

        [HarmonyPatch(typeof(AggressiveWhenSeeTarget))]
        [HarmonyPatch("IsTargetValid")]
        [HarmonyPatch(new Type[] { typeof(GameObject) })]
        internal class Patch_AggressiveWhenSeeTarget_IsTargetValid
        {
            [HarmonyPrefix]
            public static bool Prefix(AggressiveWhenSeeTarget __instance, GameObject target, ref bool __result)
            {
                FieldInfo creatureField = typeof(AggressiveWhenSeeTarget).GetField("creature", BindingFlags.NonPublic | BindingFlags.Instance);
                Creature creature = creatureField.GetValue(__instance) as Creature;

                if (crashes.Contains(creature.gameObject) && target == Player.main.gameObject)
                {
                    SubRoot subRoot = Player.main.GetCurrentSub();

                    if (
                        subRoot != null
                        && subRoot.GetComponent<Base>() == Mod.mazeBase
                        && Vector3.Distance(Player.main.transform.position, creature.transform.position) <= attackDistance
                    )
                    {
                        // Creature must have an eyeline to the player
                        Physics.Linecast(
                            creature.transform.position + new Vector3(0f, 0.75f, 0f),
                            Player.main.transform.position,
                            out RaycastHit hit
                        );

                        if (hit.collider != null && hit.collider.gameObject == Player.main.gameObject)
                        {
                            __result = true;
                            return false;
                        }
                    }
                    
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(CrashHome))]
        [HarmonyPatch("Spawn")]
        internal class Patch_CrashHome_Spawn
        {
            [HarmonyPostfix]
            public static void Postfix(CrashHome __instance)
            {
                if (crashHomes.Contains(__instance.gameObject))
                {
                    try
                    {
                        Crash crash = (Crash)crashField.GetValue(__instance);
                        crashes.Add(crash.gameObject);
                    }
                    catch (Exception e)
                    {
                        Plugin.Logger.LogInfo(e);
                        Plugin.Logger.LogInfo(e.StackTrace);
                    }
                }
            }
        }
    }
}
