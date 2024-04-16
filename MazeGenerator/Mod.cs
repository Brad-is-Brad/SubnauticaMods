using FMODUnity;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UWE;
using static Base;

namespace MazeGeneratorMod
{
    internal class Mod
    {
        public static string ModPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static bool initialized = false;

        private static Material ghostStructureMaterial = null;

        private static GameObject mazeTimerGameObject;
        public static MazeTimerLabel mazeTimerLabel;

        public static Base mazeBase = null;

        private enum PlayerLocation {
            None,
            Start,
            Maze,
            Finish
        }
        private static PlayerLocation lastPlayerLocation = PlayerLocation.None;

        private static readonly PropertyInfo builderLastRotationProperty = typeof(Builder).GetProperty("lastRotation");

        public static readonly Dictionary<TechType, GameObject> cachedPrefabs = new Dictionary<TechType, GameObject>();
        private static readonly List<TechType> relevantTechTypes = new List<TechType>()
        {
            // Maze
            TechType.BaseRoom,
            TechType.BaseCorridorI,
            TechType.BaseCorridorL,
            TechType.BaseCorridorT,
            TechType.BaseCorridorX,
            TechType.BaseHatch,
            TechType.Beacon,
            TechType.CrashHome,
            TechType.PictureFrame,
            TechType.Sign,
            TechType.PurpleBrainCoral,

            // Finish
            TechType.Flare,
            TechType.FireExtinguisher,
            TechType.PosterKitty,
            TechType.Spotlight,
        };

        public static bool oxygenEnabled = true;

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Awake")]
        public class LoadMod : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                CoroutineHost.StartCoroutine(LoadGhostMaterial());
                LoadPrefabs();

                if (mazeTimerGameObject == null)
                {
                    mazeTimerGameObject = new GameObject("Brad_is_Brad - MazeTimer");
                    mazeTimerLabel = mazeTimerGameObject.AddComponent<MazeTimerLabel>();
                    DontDestroyOnLoad(mazeTimerGameObject);
                }
            }
        }

        [HarmonyPatch(typeof(IngameMenu))]
        [HarmonyPatch("QuitGameAsync")]
        internal class Patch_IngameMenu_QuitGameAsync
        {
            [HarmonyPrefix]
            public static void Prefix(IngameMenu __instance, bool quitToDesktop)
            {
                initialized = false;
            }
        }

        [HarmonyPatch(typeof(SubRoot))]
        [HarmonyPatch("OnPlayerEntered")]
        public class Patch_SubRoot_OnPlayerEntered : MonoBehaviour
        {
            [HarmonyPostfix]
            public static void Postfix(SubRoot __instance, Player player)
            {
                if (__instance == mazeBase.GetComponent<SubRoot>())
                {
                    FMOD_StudioEventEmitter baseSound = __instance.insideSoundsRoot.GetComponent<FMOD_StudioEventEmitter>();
                    baseSound.Stop();
                }
            }
        }

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Update")]
        internal class Patch_Player_Update
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                // Features

                // TODO: parent to base so they get deleted upon quitting
                //  maybe spawn once and enable and disable the object accordingly?
                //  OR maybe override when they get written to the save file? (OnProtoSerialize)

                // TODO: make timer look better (sunbeam countdown thing?) (uGUI_SunbeamCountdown)
                // TODO: change base position (rotation?)

                // TODO: maybe make finish sign appear last?

                if (IngameMenu.main.isQuitting) { return; }

                if (!initialized)
                {
                    if (LargeWorldStreamer.main == null || LargeWorldStreamer.main.globalRoot == null) { return; }

                    GameObject globalRoot = LargeWorldStreamer.main.globalRoot;
                    foreach (Transform transform in globalRoot.transform)
                    {
                        if (transform != null && transform.position == MazeHandler.basePosition)
                        {
                            UnityEngine.Object.Destroy(transform.gameObject);
                            return;
                        }
                    }

                    if (WaitScreen.IsWaiting || cachedPrefabs.Keys.Count != relevantTechTypes.Count || ghostStructureMaterial == null) { return; }

                    initialized = true;

                    try
                    {
                        MazeHandler.DestroyMaze();
                        MazeHandler.SpawnMazeStart();
                        MazeHandler.SpawnMazeBeacon();
                        PictureFrameHandler.SpawnMazeSettingsPictureFrames();
                    }
                    catch (Exception e)
                    {
                        Plugin.Logger.LogInfo($"{e}");
                    }
                }

                FloodHandler.UpdateFloodLevel();
                MazeHandler.Update();
                PictureFrameHandler.UpdateUI();
                UpdatePlayerLocation(__instance);
            }
        }

        private static void UpdatePlayerLocation(Player player)
        {
            // Check if the player is currently in the maze
            SubRoot subRoot = player.GetCurrentSub();
            bool playerIsInMazeBase = subRoot != null && subRoot.GetComponent<Base>() == mazeBase;

            if (!playerIsInMazeBase && !MazeHandler.MazeIsGenerating())
            {
                lastPlayerLocation = PlayerLocation.None;
                mazeTimerLabel.StopTimer();
                mazeTimerLabel.Hide();
                FinishCelebrationHandler.DestroyCelebration();
                return;
            }

            Vector3 playerPosition = player.transform.position;

            if (!playerIsInMazeBase && MazeHandler.MazeIsGenerating())
            {
                FinishCelebrationHandler.DestroyCelebration();

                // Make sure we're not swimming around in the maze space while it is changing
                Vector3 min = mazeBase.GridToWorld(mazeBase.Bounds.mins + new Int3(0, -1, 0));
                Vector3 max = mazeBase.GridToWorld(mazeBase.Bounds.maxs + new Int3(0, 1, 0));
                Bounds bounds = new Bounds();
                bounds.SetMinMax(min, max);

                if (bounds.Contains(playerPosition))
                {
                    Vector3 newPosition = new Vector3(
                        playerPosition.x,
                        max.y + 2f,
                        playerPosition.z
                    );

                    player.SetPosition(newPosition);
                    return;
                }
            }

            if (!playerIsInMazeBase)
            {
                FinishCelebrationHandler.DestroyCelebration();
                return;
            }

            mazeTimerLabel.Show();

            // -- Player is in the Start area --
            if (GetStartBounds().Contains(playerPosition))
            {
                if (lastPlayerLocation == PlayerLocation.Maze)
                {
                    mazeTimerLabel.ResetTimer();
                }

                lastPlayerLocation = PlayerLocation.Start;
                player.oxygenMgr.AddOxygen(1f);

                FinishCelebrationHandler.DestroyCelebration();

                return;
            }

            if (MazeHandler.MazeIsGenerating())
            {
                // Player should not be in the maze while it is changing
                player.SetPosition(MazeHandler.basePosition + new Vector3(5f, 0, 5f));

                FinishCelebrationHandler.DestroyCelebration();

                return;
            }

            Int3 finishCell = mazeBase.WorldToGrid(MazeHandler.finishPosition);

            Vector3 finishMin = mazeBase.GridToWorld(finishCell + new Int3(0, -1, 0));
            Vector3 finishMax = mazeBase.GridToWorld(finishCell + new Int3(2, 1, 3));

            Bounds finishBounds = new Bounds();
            finishBounds.SetMinMax(finishMin, finishMax);

            // -- Player is in the Finish area --
            if (finishBounds.Contains(playerPosition))
            {
                lastPlayerLocation = PlayerLocation.Finish;
                mazeTimerLabel.StopTimer();
                player.oxygenMgr.AddOxygen(1f);

                FinishCelebrationHandler.SpawnCelebration(
                    MazeHandler.finishPosition + new Vector3(5f, 0f, 5f)
                );

                return;
            }

            FinishCelebrationHandler.DestroyCelebration();

            // -- Player is in the maze --
            if (oxygenEnabled)
            {
                player.oxygenMgr.AddOxygen(1f);
            }

            if (lastPlayerLocation == PlayerLocation.Start)
            {
                mazeTimerLabel.ResetTimer();
                mazeTimerLabel.StartTimer();
            }

            lastPlayerLocation = PlayerLocation.Maze;
        }

        public static Bounds GetStartBounds()
        {
            Int3 startCell = mazeBase.WorldToGrid(MazeHandler.basePosition);

            Vector3 startMin = mazeBase.GridToWorld(startCell + new Int3(0, -1, -1));
            Vector3 startMax = mazeBase.GridToWorld(startCell + new Int3(2, 1, 2));

            Bounds startBounds = new Bounds();
            startBounds.SetMinMax(startMin, startMax);

            return startBounds;
        }

        public static FMOD.Studio.EventInstance CreateSound(string path, bool play)
        {
            // To stop:
            // sound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);

            FMOD.Studio.EventInstance sound = RuntimeManager.CreateInstance(path);
            sound.set3DAttributes(RuntimeUtils.To3DAttributes(MainCamera.camera.transform.position));

            if (play)
            {
                sound.start();
                sound.release();
            }
            
            return sound;
        }

        private static void LoadPrefabs()
        {
            foreach (TechType techType in relevantTechTypes)
            {
                LoadTechTypePrefab(techType);
            }
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

        private static void LoadTechTypePrefab(TechType techType)
        {
            IEnumerator spawnTechType = SpawnTechTypeAsync(techType);
            CoroutineHost.StartCoroutine(spawnTechType);
            return;
        }

        private static IEnumerator LoadGhostMaterial()
        {
            AsyncOperationHandle<Material> resourceRequest2 = AddressablesUtility.LoadAsync<Material>("Materials/ghostmodel.mat");
            yield return resourceRequest2;
            resourceRequest2.LogExceptionIfFailed("Materials/ghostmodel.mat");
            ghostStructureMaterial = resourceRequest2.Result;
        }

        private static void AssignGhostMaterial(GameObject ghostModel)
        {
            MaterialExtensions.AssignMaterial(ghostModel, ghostStructureMaterial, includeDisabled: true);

            Color color = new Color(0f, 1f, 0f, 1f); // (canPlace ? placeColorAllow : placeColorDeny);
            IBuilderGhostModel[] components = ghostModel.GetComponents<IBuilderGhostModel>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].UpdateGhostModelColor(true, ref color);
            }

            ghostStructureMaterial.SetColor(ShaderPropertyID._Tint, color);
        }

        public static void PlaceBasePrefab(GameObject prefab, Vector3 position, int degreesRotated)
        {
            PlaceBasePrefab(
                prefab,
                position,
                degreesRotated,
                new Face()
            );
        }

        public static void PlaceBasePrefab(GameObject prefab, Vector3 position, int degreesRotated, Face face)
        {
            builderLastRotationProperty.SetValue(null, degreesRotated / 90, BindingFlags.NonPublic | BindingFlags.Static, null, null, null);

            ConstructableBase constructableBaseComponent = UnityEngine.Object.Instantiate(prefab).GetComponent<ConstructableBase>();
            GameObject ghostModel = constructableBaseComponent.model;

            BaseGhost baseGhost = ghostModel.GetComponent<BaseGhost>();
            baseGhost.SetupGhost();

            AssignGhostMaterial(ghostModel);

            ConstructableBase componentInParent = baseGhost.GetComponentInParent<ConstructableBase>();

            // we must move the parent while the child is in its original position, as they are already attached
            componentInParent.transform.position = MazeHandler.basePosition;
            componentInParent.transform.rotation = MazeHandler.baseRotation;

            FieldInfo targetBaseField = typeof(BaseGhost).GetField("targetBase", BindingFlags.NonPublic | BindingFlags.Instance);

            if (mazeBase != null)
            {
                componentInParent.transform.position = position; // existingBase.GridToWorld(offset);
                componentInParent.transform.rotation = mazeBase.transform.rotation;

                targetBaseField.SetValue(baseGhost, mazeBase);
            }

            BaseAddFaceGhost baseAddFaceGhost = baseGhost.GetComponent<BaseAddFaceGhost>();
            if (baseAddFaceGhost != null)
            {
                Int3 @int = (baseAddFaceGhost.targetOffset = baseAddFaceGhost.TargetBase.NormalizeCell(face.cell));
                CellType cell = baseAddFaceGhost.TargetBase.GetCell(@int);
                Int3 int2 = CellSize[(uint)cell];
                Int3.Bounds a = new Int3.Bounds(face.cell, face.cell);
                Int3.Bounds b = new Int3.Bounds(@int, @int + int2 - 1);
                Int3.Bounds sourceRange = Int3.Bounds.Union(a, b);

                // private bool UpdateSize(Int3 size)
                MethodInfo updateSizeMethod = typeof(BaseAddFaceGhost).GetMethod("UpdateSize", BindingFlags.NonPublic | BindingFlags.Instance);
                updateSizeMethod.Invoke(baseAddFaceGhost, new object[] { sourceRange.size });

                // protected Base ghostBase;
                FieldInfo ghostBaseField = typeof(BaseGhost).GetField("ghostBase", BindingFlags.NonPublic | BindingFlags.Instance);
                Base ghostBase = (Base)ghostBaseField.GetValue(baseAddFaceGhost);

                Face face2 = new Face(face.cell - baseAddFaceGhost.TargetBase.GetAnchor(), face.direction);
                if (!baseAddFaceGhost.anchoredFace.HasValue || baseAddFaceGhost.anchoredFace.Value != face2)
                {
                    baseAddFaceGhost.anchoredFace = face2;
                    ghostBase.CopyFrom(baseAddFaceGhost.TargetBase, sourceRange, sourceRange.mins * -1);
                    ghostBase.ClearMasks();
                    Int3 cell2 = face.cell - @int;
                    Face face3 = new Face(cell2, face.direction);
                    ghostBase.SetFaceMask(face3, isMasked: true);
                    ghostBase.SetFaceType(face3, Base.FaceType.Hatch);
                    baseAddFaceGhost.RebuildGhostGeometry();
                }

                componentInParent.transform.position = baseAddFaceGhost.TargetBase.GridToWorld(@int);
                componentInParent.transform.rotation = baseAddFaceGhost.TargetBase.transform.rotation;
            }

            baseGhost.Finish();

            if (mazeBase == null)
            {
                // This must happen after the Finish() method is called on the first base piece
                mazeBase = (Base)targetBaseField.GetValue(baseGhost);
            }

            UnityEngine.Object.Destroy(ghostModel);
            UnityEngine.Object.Destroy(baseGhost);
            UnityEngine.Object.Destroy(constructableBaseComponent);
        }

        public static void PrintComponents(Component target, string label, bool printParentComps, bool printComps, bool printChildComps)
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
