using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DriveAnythingMod
{
    internal class DriveAnythingHandler
    {
        static List<Collider> combinedColliders = new List<Collider>(); // Scanner room colliders, for example
        static List<Rigidbody> disabledRigidbodies = new List<Rigidbody>();

        static GameObject sittingPosition;
        static GameObject drivingGameObject;
        static Base movingBase;
        static Rigidbody rigidbody;
        static PilotingChair chair;
        static SubControl subControl;
        static bool drivingBase = false;
        static List<BaseCamera> baseCameras = new List<BaseCamera>();
        static LargeWorldEntity.CellLevel? originalCellLevel = null;

        static GameObject originalRigidbodySettingsGameObject;
        static Rigidbody originalRigidbodySettings;

        public static bool freezePiloting = false;

        static List<string> forbiddenGameObjects = new List<string> {
            "Player",
            "ChunkCollider",
            "CellRoot",
            "Batch",
            "Normal",
            "Sphere",
            "Caves",
            "Geyser",
            "Streaming",
            "Atmosphere",
            "Volume",
            "Global Root"
        };

        static MethodInfo clearAllChunksMethod = typeof(MiniWorld).GetMethod("ClearAllChunks", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo subscribeMethod = typeof(PilotingChair).GetMethod("Subscribe", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo currentPlayerField = typeof(PilotingChair).GetField("currentPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo cameraField = typeof(MapRoomCameraDocking).GetField("camera", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo currChairField = typeof(Player).GetField("currChair", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo modeField = typeof(Player).GetField("mode", BindingFlags.NonPublic | BindingFlags.Instance);

        private class BaseCamera
        {
            public MapRoomCameraDocking dock;
            public MapRoomCamera camera;
            public Transform originalParent;

            public BaseCamera(MapRoomCameraDocking dock, MapRoomCamera camera, Transform originalParent)
            {
                this.dock = dock;
                this.camera = camera;
                this.originalParent = originalParent;
            }
        }

        public static GameObject GetLookingAt()
        {
            try
            {
                Vector3 position = Camera.main.transform.position;
                int num = UWE.Utils.SpherecastIntoSharedBuffer(
                    position,
                    1.0f, //spherecastRadius
                    Camera.main.transform.forward,
                    Plugin.config.maxLookDistance
                );

                for (int i = 0; i < num; i++)
                {
                    RaycastHit raycastHit = UWE.Utils.sharedHitBuffer[i];

                    Mod.LogDebug($"SphereCast raycastHit - {raycastHit.collider.gameObject}");
                    GameObject rootGameObject = raycastHit.collider.gameObject;

                    if (
                        (
                            rootGameObject.name.Contains("Landscape")
                            || rootGameObject.name.Contains("SerializerEmptyGameObject")
                        )
                    )
                    {
                        Mod.LogDebug($"Grabbed landscape but not in CHAOS MODE...");
                        continue;
                    }

                    bool isForbidden = false;
                    foreach (string forbiddenGameObject in forbiddenGameObjects)
                    {
                        if (rootGameObject.name.Contains(forbiddenGameObject))
                        {
                            Mod.LogDebug($"Grabbed forbidden: {rootGameObject}");
                            isForbidden = true;
                            break;
                        }
                    }
                    if (isForbidden) continue;

                    for (int j = 0; j < 10; j++)
                    {
                        if (
                            rootGameObject == null
                            || rootGameObject.transform == null
                            || rootGameObject.transform.parent == null
                            || rootGameObject.transform.parent.gameObject == null
                            || rootGameObject == rootGameObject.transform.parent.gameObject
                        )
                            break;

                        if (rootGameObject.name.Contains("Base") && rootGameObject.transform.parent.gameObject.name.Contains("Base"))
                        {
                            Mod.LogDebug($"Parent is also base: {rootGameObject} -  {rootGameObject.transform.parent.gameObject}");
                            break;
                        }

                        bool isParentForbidden = false;
                        foreach (string forbiddenGameObject in forbiddenGameObjects)
                        {
                            if (rootGameObject.transform.parent.gameObject.name.Contains(forbiddenGameObject))
                            {
                                Mod.LogDebug($"Parent forbidden: {rootGameObject.transform.parent.gameObject}");
                                isParentForbidden = true;
                                break;
                            }
                        }
                        if (isParentForbidden) break;

                        if (
                            (
                                rootGameObject.transform.parent.gameObject.name.Contains("Landscape")
                                || rootGameObject.transform.parent.gameObject.name.Contains("SerializerEmptyGameObject")
                            )
                        )
                        {
                            Mod.LogDebug($"Parent contains landscape but not in CHAOS MODE...");
                            break;
                        }

                        rootGameObject = rootGameObject.transform.parent.gameObject;
                    }

                    return rootGameObject;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"{e}");
            }

            return null;
        }

        public static void UpdateLookingAt()
        {
            if (drivingBase) return;
            if (Mod.infoLabel == null) return;

            GameObject lookingAt = GetLookingAt();
            Mod.infoLabel.debugInfoString = $"Looking at: {lookingAt}";
        }

        public static void BeginPilot()
        {
            if (drivingBase || Player.main.isPiloting) return;

            Plugin.Logger.LogInfo($"BeginPilot");
            SubRoot subRoot = Player.main.GetCurrentSub();
            if (subRoot != null && subRoot.isBase)
            {
                PilotCurrentBase();
            }
            else
            {
                PilotCurrentObject();
            }
        }

        static void ApplyRigidbodySettings(GameObject drivingGameObject)
        {
            originalRigidbodySettings = null;

            rigidbody = drivingGameObject.GetComponent<Rigidbody>();
            if (!rigidbody)
            {
                // Create rigidbody
                rigidbody = drivingGameObject.AddComponent<Rigidbody>();

            }
            else
            {
                if (originalRigidbodySettingsGameObject != null)
                {
                    UnityEngine.Object.Destroy(originalRigidbodySettingsGameObject);
                }

                originalRigidbodySettingsGameObject = new GameObject();
                originalRigidbodySettings = originalRigidbodySettingsGameObject.AddComponent<Rigidbody>();
                originalRigidbodySettings.mass = rigidbody.mass;
                originalRigidbodySettings.angularDrag = rigidbody.angularDrag;
                originalRigidbodySettings.drag = rigidbody.drag;
                originalRigidbodySettings.centerOfMass = rigidbody.centerOfMass;
                originalRigidbodySettings.inertiaTensor = rigidbody.inertiaTensor;
                originalRigidbodySettings.inertiaTensorRotation = rigidbody.inertiaTensorRotation;
                originalRigidbodySettings.interpolation = rigidbody.interpolation;
                originalRigidbodySettings.useGravity = rigidbody.useGravity;
            }

            rigidbody.mass = Plugin.config.rigidbodyMass;
            rigidbody.angularDrag = Plugin.config.rigidbodyAngularDrag;
            rigidbody.drag = Plugin.config.rigidbodyDrag;
            rigidbody.centerOfMass = Vector3.zero;
            rigidbody.inertiaTensor = new Vector3(
                Plugin.config.rigidbodyInertiaTensor[0],
                Plugin.config.rigidbodyInertiaTensor[1],
                Plugin.config.rigidbodyInertiaTensor[2]
            );
            rigidbody.inertiaTensorRotation = Quaternion.identity;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.useGravity = false;
        }

        static void RestoreRigidbodySettings()
        {
            if (originalRigidbodySettings != null)
            {
                rigidbody.mass = originalRigidbodySettings.mass;
                rigidbody.angularDrag = originalRigidbodySettings.angularDrag;
                rigidbody.drag = originalRigidbodySettings.drag;
                rigidbody.centerOfMass = originalRigidbodySettings.centerOfMass;
                rigidbody.inertiaTensor = originalRigidbodySettings.inertiaTensor;
                rigidbody.inertiaTensorRotation = originalRigidbodySettings.inertiaTensorRotation;
                rigidbody.interpolation = originalRigidbodySettings.interpolation;
                rigidbody.useGravity = originalRigidbodySettings.useGravity;
            }
            else
            {
                UnityEngine.Object.Destroy(rigidbody);
            }

            rigidbody = null;
        }

        private static void PilotCurrentObject()
        {
            // TODO: add steering wheel?
            // TODO: allow and disallow lists

            Plugin.Logger.LogInfo($"PilotCurrentObject");

            drivingGameObject = GetLookingAt();
            if (drivingGameObject == null) return;

            Plugin.Logger.LogInfo($"PilotCurrentObject - {drivingGameObject}");

            // Prevent from de-spawning and freezing the game
            originalCellLevel = null;
            LargeWorldEntity largeWorldEntity = drivingGameObject.GetComponent<LargeWorldEntity>();
            if (largeWorldEntity != null)
            {
                originalCellLevel = largeWorldEntity.cellLevel;
                largeWorldEntity.cellLevel = LargeWorldEntity.CellLevel.Global;
                if ((bool)LargeWorldStreamer.main && LargeWorldStreamer.main.cellManager != null)
                {
                    LargeWorldStreamer.main.cellManager.RegisterEntity(largeWorldEntity);
                }
            }

            // Don't drive a base from the outside
            Base baseComponent = drivingGameObject.GetComponentInParent<Base>();
            if (baseComponent != null) { return; }

            // Create sitting position
            sittingPosition = new GameObject();
            sittingPosition.transform.position = Camera.main.transform.position;
            sittingPosition.transform.rotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
            sittingPosition.transform.parent = drivingGameObject.transform;

            ApplyRigidbodySettings(drivingGameObject);

            // Create a piloting chair
            chair = drivingGameObject.AddComponent<PilotingChair>();
            chair.sittingPosition = sittingPosition.transform;

            currentPlayerField.SetValue(chair, Player.main);

            // TODO: This could probably be done more efficiently
            Component[] components = drivingGameObject.GetComponentsInChildren<Component>();
            foreach (Component component in components)
            {
                if (
                    component.name.Contains("Pillar")
                    || component.name.Contains("Support")
                    || component.name == "Joint"
                    || component.name == "Leg"
                    || component.name == "foot"
                )
                {
                    Mod.LogDebug($"Destroying - {component.transform.position} - {component}");
                    UnityEngine.Object.Destroy(component);
                }
                else if (component is BoxCollider && component.name == "adjustable")
                {
                    // This was causing the MoonPool
                    // and maybe Scanner Room to catch on the ground
                    UnityEngine.Object.Destroy(component);
                }
                else if (component is MapRoomCameraDocking)
                {
                    MapRoomCamera mapRoomCamera = (MapRoomCamera)cameraField.GetValue(component);
                    if (mapRoomCamera != null)
                    {
                        baseCameras.Add(new BaseCamera((MapRoomCameraDocking)component, mapRoomCamera, mapRoomCamera.transform.parent));
                        mapRoomCamera.transform.parent = drivingGameObject.transform;
                        ((MapRoomCameraDocking)component).DockCamera(mapRoomCamera);
                    }
                }
                else if (component is Collider)
                {
                    Collider collider = (Collider)component;
                    if (collider.enabled && !collider.isTrigger && component.name == "CombinedCollider")
                    {
                        collider.enabled = false;
                        combinedColliders.Add(collider);
                    }
                }
            }

            //_currentSubField.SetValue(Player.main, subRoot);
            drivingBase = true;
            //Player.main.EnterPilotingMode(chair);
            currChairField.SetValue(Player.main, chair);
            Player.main.cinematicModeActive = true;
            MainCameraControl.main.lookAroundMode = true;

            Player.main.transform.parent = chair.sittingPosition.transform;
            UWE.Utils.ZeroTransform(Player.main.transform);
            subControl = drivingGameObject.AddComponent<SubControl>();
            subControl.Set(SubControl.Mode.DirectInput);
            modeField.SetValue(Player.main, Player.Mode.Piloting);
            Inventory.main.quickSlots.DeselectImmediate();
            Player.main.playerModeChanged.Trigger(Player.Mode.Piloting);

            subscribeMethod.Invoke(chair, new object[] { Player.main, true });
        }

        private static void PilotCurrentBase()
        {
            Plugin.Logger.LogInfo($"PilotCurrentBase");

            // TODO: Make sure this class never breaks anything
            // FreezeRigidbodyWhenFar dsfds;

            SubRoot subRoot = Player.main.GetCurrentSub();
            if (!subRoot.isBase) { return; }

            // Create sitting position
            sittingPosition = new GameObject();
            sittingPosition.transform.position = Camera.main.transform.position;
            sittingPosition.transform.rotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
            sittingPosition.transform.parent = subRoot.transform;

            // Get references to Base
            drivingGameObject = subRoot.gameObject;
            movingBase = subRoot.GetComponent<Base>();

            // Create rigidbody
            rigidbody = drivingGameObject.AddComponent<Rigidbody>();
            rigidbody.mass = Plugin.config.rigidbodyMass;
            rigidbody.angularDrag = Plugin.config.rigidbodyAngularDrag;
            rigidbody.drag = Plugin.config.rigidbodyDrag;
            rigidbody.centerOfMass = Vector3.zero;
            rigidbody.inertiaTensor = new Vector3(
                Plugin.config.rigidbodyInertiaTensor[0],
                Plugin.config.rigidbodyInertiaTensor[1],
                Plugin.config.rigidbodyInertiaTensor[2]
            );
            rigidbody.inertiaTensorRotation = Quaternion.identity;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.useGravity = false;

            // Create a piloting chair
            chair = drivingGameObject.AddComponent<PilotingChair>();
            chair.transform.position = sittingPosition.transform.position;
            //chair.transform.rotation = sittingPosition.transform.rotation;
            chair.sittingPosition = sittingPosition.transform;

            currentPlayerField.SetValue(chair, Player.main);

            subControl = subRoot.gameObject.AddComponent<SubControl>();

            // Add rigidbody constraints (doesn't seem to work)
            rigidbody.constraints =
                RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ
            ;

            // TODO: This could probably be done more efficiently
            Component[] components = drivingGameObject.GetComponentsInChildren<Component>();
            foreach (Component component in components)
            {
                if (
                    component.name.Contains("Pillar")
                    || component.name.Contains("Support")
                    || component.name == "Joint"
                    || component.name == "Leg"
                    || component.name == "foot"
                )
                {
                    //Plugin.Logger.LogInfo($"Destroying - {component.transform.position} - {component}");
                    UnityEngine.Object.Destroy(component);
                }
                else if (component is BoxCollider && component.name == "adjustable")
                {
                    // This was causing the MoonPool
                    // and maybe Scanner Room to catch on the ground
                    UnityEngine.Object.Destroy(component);
                }
                else if (component is MapRoomCameraDocking)
                {
                    MapRoomCamera mapRoomCamera = (MapRoomCamera)cameraField.GetValue(component);
                    if (mapRoomCamera != null)
                    {
                        baseCameras.Add(new BaseCamera((MapRoomCameraDocking)component, mapRoomCamera, mapRoomCamera.transform.parent));
                        mapRoomCamera.transform.parent = drivingGameObject.transform;
                        ((MapRoomCameraDocking)component).DockCamera(mapRoomCamera);
                    }
                }
                else if (component is Collider)
                {
                    Collider collider = (Collider)component;
                    if (collider.enabled && !collider.isTrigger && component.name == "CombinedCollider")
                    {
                        collider.enabled = false;
                        combinedColliders.Add(collider);
                        //Plugin.Logger.LogInfo($"Collider - {component} - {collider.enabled} - {collider.isTrigger}");
                    }
                }
                else if (component is Rigidbody && component != rigidbody && ((Rigidbody)component).detectCollisions)
                {
                    disabledRigidbodies.Add((Rigidbody)component);
                    ((Rigidbody)component).detectCollisions = false;
                }
                else
                {
                    // Maybe print if debug mode
                    //Plugin.Logger.LogInfo($"Keeping - {component.transform.position} - {component}");
                }
            }

            Player.main.EnterPilotingMode(chair);
            drivingBase = true;

            subscribeMethod.Invoke(chair, new object[] { Player.main, true });
        }

        public static void Update()
        {
            UpdateLookingAt();

            if (
                drivingBase
                && AvatarInputHandler.main != null
                && AvatarInputHandler.main.IsEnabled()
                && GameInput.GetButtonDown(GameInput.Button.Exit)
            )
            {
                StopPiloting();
            }
        }

        static void StopPiloting()
        {
            Plugin.Logger.LogInfo("Ejecting player...");

            GameInput.ClearInput();
            Player.main.transform.parent = null;
            MainCameraControl.main.lookAroundMode = false;

            Player.main.armsController.SetWorldIKTarget(null, null);
            GameObject entityRoot = UWE.Utils.GetEntityRoot(chair.gameObject);
            if (entityRoot != null)
            {
                entityRoot.BroadcastMessage("StopPiloting");
            }

            subControl.Set(SubControl.Mode.GameObjects);
            modeField.SetValue(Player.main, Player.Mode.Normal);
            currChairField.SetValue(Player.main, null);
            Player.main.playerModeChanged.Trigger(Player.Mode.Normal);

            Player.main.cinematicModeActive = false;
            drivingBase = false;

            RestoreRigidbodySettings();

            UnityEngine.Object.Destroy(sittingPosition);
            UnityEngine.Object.Destroy(chair);
            UnityEngine.Object.Destroy(subControl);

            // Fix scanner room map
            if (movingBase != null)
            {
                movingBase.RebuildGeometry();

                MapRoomFunctionality[] mapRoomFunctionalities = drivingGameObject.GetComponentsInChildren<MapRoomFunctionality>();
                foreach (var mapRoomFunctionality in mapRoomFunctionalities)
                {
                    clearAllChunksMethod.Invoke(mapRoomFunctionality.miniWorld, new object[] { });
                }

                movingBase = null;
            }

            // Put scanner room cameras back in place
            foreach (var baseCamera in baseCameras)
            {
                baseCamera.camera.transform.parent = baseCamera.originalParent;
                baseCamera.dock.DockCamera(baseCamera.camera);
            }
            baseCameras.Clear();

            // Re-enable colliders
            foreach (var combinedCollider in combinedColliders)
            {
                combinedCollider.enabled = true;
            }
            combinedColliders.Clear();

            // Re-enabled rigidbodies
            foreach (var disabledRigidbody in disabledRigidbodies)
            {
                disabledRigidbody.detectCollisions = true;
            }
            disabledRigidbodies.Clear();

            // Restore original cell level
            LargeWorldEntity largeWorldEntity = drivingGameObject.GetComponent<LargeWorldEntity>();
            if (largeWorldEntity != null && originalCellLevel != null)
            {
                largeWorldEntity.cellLevel = (LargeWorldEntity.CellLevel)originalCellLevel;
                if ((bool)LargeWorldStreamer.main && LargeWorldStreamer.main.cellManager != null)
                {
                    LargeWorldStreamer.main.cellManager.RegisterEntity(largeWorldEntity);
                }
            }
            originalCellLevel = null;

            drivingGameObject = null;

            Plugin.Logger.LogInfo("Ejected player.");
        }

        public static void FixedUpdate()
        {
            if (freezePiloting == true) return;

            if (drivingBase && rigidbody != null)
            {
                Quaternion rotationBefore = rigidbody.transform.rotation;

                // Limit velocity
                if (rigidbody.velocity.magnitude > 50f)
                {
                    rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, 50f);
                }

                Vector3 throttle = GameInput.GetMoveDirection();

                // Turn left and right
                rigidbody.AddTorque(
                    new Vector3(
                        0f,
                        Plugin.config.turnSpeed * throttle.x * Time.fixedDeltaTime,
                        0f
                    ),
                    ForceMode.Acceleration
                );

                // Ignore camera's vertical rotation
                Vector3 flatForward = new Vector3(
                    sittingPosition.transform.forward.x,
                    0f,
                    sittingPosition.transform.forward.z
                );

                Vector3 forceDirection =
                    flatForward * throttle.z
                    + Vector3.up * throttle.y
                ;

                Vector3 totalForce = forceDirection * Plugin.config.drivingForce * Time.fixedDeltaTime;
                Vector3 clampedForce = Vector3.ClampMagnitude(totalForce, 100f);

                rigidbody.AddForce(clampedForce, ForceMode.Acceleration);

                if (Camera.main.transform.position.y > 0f)
                {
                    rigidbody.useGravity = Plugin.config.rigidbodyUseGravityAboveZero;
                }
                else
                {
                    rigidbody.useGravity = Plugin.config.rigidbodyUseGravityBelowZero;
                }

                // Limit rotation to only Y rotation
                Creature creature = drivingGameObject.GetComponent<Creature>();
                if (creature == null)
                {
                    rigidbody.transform.rotation = rotationBefore;
                }
            }
        }
    }
}
