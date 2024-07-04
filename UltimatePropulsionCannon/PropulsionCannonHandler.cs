using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UltimatePropulsionCannon
{
    internal class PropulsionCannonHandler
    {
        static float minMass = 10f;
        static float forceMultiplier = 5f;
        static float velocity = 100f;
        static float spherecastRadius = 1.0f;
        static GameObject lastGrabbedObject = null;

        static GameObject lastTraced = null;
        static List<string> forbiddenGameObjects = new List<string> {
            "Player",
            "ChunkCollider",
            "CellRoot",
            "Batch",
            "Normal",
            "Sphere",
            "Caves",
            "Geyser",
        };

        [HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("Start")]
        public class Patch_PropulsionCannon_Start : MonoBehaviour
        {
            [HarmonyPostfix]
            public static void Postfix(PropulsionCannon __instance)
            {
                FieldInfo bannedTechField = typeof(PropulsionCannon)
                        .GetField("bannedTech", BindingFlags.NonPublic | BindingFlags.Static);

                HashSet<TechType> bannedTech = (HashSet<TechType>)bannedTechField.GetValue(null);
                bannedTech.Clear();

                //__instance.massScalingFactor = 0.02f * multiplier;
                __instance.pickupDistance = Plugin.config.pickupDistance; // 18f;
                __instance.maxMass = Plugin.config.maxMass; // 1200f;
                __instance.maxAABBVolume = Plugin.config.maxAABBVolume; // 120f;
            }
        }

        /*[HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("ValidateObject")]
        public class Patch_PropulsionCannon_ValidateObject : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(PropulsionCannon __instance, GameObject go, bool __result)
            {
                try
                {
                    if (!go.activeSelf || !go.activeInHierarchy)
                    {
                        Mod.LogDebug($"ValidateObject false: object is inactive");
                        return;
                    }

                    Rigidbody component = go.GetComponent<Rigidbody>();
                    if (component == null)
                    {
                        Mod.LogDebug($"ValidateObject false: no Rigidbody");
                        return;
                    }
                    else if (component.mass > __instance.maxMass)
                    {
                        Mod.LogDebug($"ValidateObject false: too massive {component.mass} > {__instance.maxMass}");
                        return;
                    }

                    Pickupable component2 = go.GetComponent<Pickupable>();
                    bool flag = false;
                    if (component2 != null)
                    {
                        flag = component2.attached;
                        Mod.LogDebug($"ValidateObject: Pickupable flag: {flag}");
                    }

                    MethodInfo isAllowedToGrabMethod = typeof(PropulsionCannon).GetMethod("IsAllowedToGrab", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool isAllowedToGrab = (bool)isAllowedToGrabMethod.Invoke(__instance, new object[] { go });
                    if (!isAllowedToGrab)
                    {
                        Mod.LogDebug($"ValidateObject false: not allowed to grab");
                        return;
                    }

                    if (!__instance.energyInterface.hasCharge)
                    {
                        Mod.LogDebug($"ValidateObject false: energy interface has no charge");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"ValidateObject: {e}");
                }
            }
        }*/

        [HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("FixedUpdate")]
        public class Patch_PropulsionCannon_FixedUpdate : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(PropulsionCannon __instance)
            {
                try
                {
                    if (__instance == null) return;

                    if (__instance.energyInterface != null)
                        __instance.energyInterface.AddEnergy(100f);

                    if (__instance.grabbedObject != null)
                    {
                        Rigidbody component = __instance.grabbedObject.GetComponent<Rigidbody>();
                        if (component != null)
                        {
                            __instance.shootForce = Math.Max(component.mass, minMass) * forceMultiplier;
                            __instance.attractionForce = Math.Max(component.mass, minMass) * forceMultiplier;
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"PropulsionCannon.FixedUpdate - {e}");
                }
            }
        }

        [HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("OnShoot")]
        public class Patch_PropulsionCannon_OnShoot : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(PropulsionCannon __instance)
            {
                try
                {
                    Mod.LogDebug($"OnShoot! - {__instance.grabbedObject}");
                    if (__instance.grabbedObject == null) { return; }

                    // private Bounds GetAABB(GameObject target)
                    MethodInfo getAABBMethod = typeof(PropulsionCannon).GetMethod("GetAABB", BindingFlags.NonPublic | BindingFlags.Instance);
                    Bounds aABB = (Bounds)getAABBMethod.Invoke(__instance, new object[] { __instance.grabbedObject });

                    bool result = aABB.size.x * aABB.size.y * aABB.size.z <= __instance.maxAABBVolume;
                    lastGrabbedObject = __instance.grabbedObject;
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"{e}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(PropulsionCannon __instance)
            {
                try
                {
                    if (lastGrabbedObject == null || __instance.grabbedObject != null) return;

                    Rigidbody component = lastGrabbedObject.GetComponent<Rigidbody>();
                    if (component != null)
                    {
                        if (component.constraints != RigidbodyConstraints.None) { component.constraints = RigidbodyConstraints.None; }
                        if (component.drag > 0.5f) { component.drag = 0.5f; }
                        if (component.angularDrag > 0.5f) { component.angularDrag = 0.5f; }

                        MethodInfo getAABBMethod = typeof(PropulsionCannon).GetMethod("GetAABB", BindingFlags.NonPublic | BindingFlags.Instance);
                        Bounds aABB = (Bounds)getAABBMethod.Invoke(__instance, new object[] { lastGrabbedObject });

                        component.velocity = Camera.main.transform.forward * velocity * (aABB.size.magnitude > 200f ? aABB.size.magnitude / 200f : 1f);
                        lastGrabbedObject = null;
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"{e}");
                }
            }
        }

        [HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("CheckLineOfSight")]
        public class Patch_PropulsionCannon_CheckLineOfSight : MonoBehaviour
        {
            [HarmonyPostfix]
            public static void Postfix(PropulsionCannon __instance, bool __result, GameObject obj, Vector3 a, Vector3 b)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("GrabObject")]
        public class Patch_PropulsionCannon_GrabObject : MonoBehaviour
        {
            [HarmonyPrefix]
            public static void Prefix(PropulsionCannon __instance, GameObject target)
            {
                Rigidbody component = target.GetComponent<Rigidbody>();
                if (component == null)
                {
                    component = target.AddComponent<Rigidbody>();
                    component.mass = minMass;
                    component.drag = 0f;
                    component.angularDrag = 0.05f;
                    Mod.LogDebug($"GrabObject - Added rigidbody to {target}");
                }
                else
                {
                    //Mod.LogDebug($"GrabObject - {target} rigidbody - mass - {component.mass}");
                    //Mod.LogDebug($"GrabObject - {target} rigidbody - drag - {component.drag}");
                    //Mod.LogDebug($"GrabObject - {target} rigidbody - angularDrag - {component.angularDrag}");
                    //Mod.LogDebug($"GrabObject - {target} rigidbody - useGravity - {component.useGravity}");
                    //Mod.LogDebug($"GrabObject - {target} rigidbody - constraints - {component.constraints}");

                    if (component.constraints != RigidbodyConstraints.None) { component.constraints = RigidbodyConstraints.None; }
                    if (component.drag > 0.5f) { component.drag = 0.5f; }
                    if (component.angularDrag > 0.5f) { component.angularDrag = 0.5f; }
                }
            }
        }

        [HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("TraceForGrabTarget")]
        public class Patch_PropulsionCannon_TraceForGrabTarget : MonoBehaviour
        {
            [HarmonyPrefix]
            public static bool Prefix(PropulsionCannon __instance, GameObject __result)
            {
                try
                {
                    // private static List<GameObject> checkedObjects = new List<GameObject>();
                    FieldInfo checkedObjectsField = typeof(PropulsionCannon)
                            .GetField("checkedObjects", BindingFlags.NonPublic | BindingFlags.Static);
                    List<GameObject> checkedObjects = (List<GameObject>)checkedObjectsField.GetValue(null);

                    // private HashSet<GameObject> launchedObjects = new HashSet<GameObject>();
                    FieldInfo launchedObjectsField = typeof(PropulsionCannon)
                            .GetField("launchedObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                    HashSet<GameObject> launchedObjects = (HashSet<GameObject>)launchedObjectsField.GetValue(__instance);

                    Vector3 position = MainCamera.camera.transform.position;
                    //int layerMask = ~(1 << LayerMask.NameToLayer("Player"));
                    int num = UWE.Utils.SpherecastIntoSharedBuffer(
                        position,
                        spherecastRadius,
                        MainCamera.camera.transform.forward,
                        __instance.pickupDistance
                    );
                    GameObject result = null;
                    float num2 = float.PositiveInfinity;
                    checkedObjects.Clear();

                    for (int i = 0; i < num; i++)
                    {
                        RaycastHit raycastHit = UWE.Utils.sharedHitBuffer[i];

                        //Mod.LogDebug($"SphereCast raycastHit - {raycastHit.collider.gameObject}");
                        GameObject rootGameObject = raycastHit.collider.gameObject;

                        if (
                            (
                                rootGameObject.name.Contains("Landscape")
                                || rootGameObject.name.Contains("SerializerEmptyGameObject")
                            )
                            && !Plugin.config.enableLandscape
                        )
                        {
                            //Mod.LogDebug($"Grabbed landscape but not in CHAOS MODE...");
                            continue;
                        }

                        bool isForbidden = false;
                        foreach (string forbiddenGameObject in forbiddenGameObjects)
                        {
                            if (rootGameObject.name.Contains(forbiddenGameObject))
                            {
                                //Mod.LogDebug($"Grabbed forbidden: {rootGameObject}");
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
                                //Mod.LogDebug($"Parent is also base: {rootGameObject} -  {rootGameObject.transform.parent.gameObject}");
                                break;
                            }

                            bool isParentForbidden = false;
                            foreach (string forbiddenGameObject in forbiddenGameObjects)
                            {
                                if (rootGameObject.transform.parent.gameObject.name.Contains(forbiddenGameObject))
                                {
                                    //Mod.LogDebug($"Parent forbidden: {rootGameObject.transform.parent.gameObject}");
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
                                && !Plugin.config.enableLandscape
                            )
                            {
                                //Mod.LogDebug($"Parent contains landscape but not in CHAOS MODE...");
                                break;
                            }

                            rootGameObject = rootGameObject.transform.parent.gameObject;
                        }

                        GameObject entityRoot = rootGameObject;

                        //Mod.LogDebug($"TraceForGrabTarget - entityRoot - {entityRoot}");

                        if (!launchedObjects.Contains(entityRoot))
                        {
                            float sqrMagnitude = (raycastHit.point - position).sqrMagnitude;
                            if (sqrMagnitude < num2 && true)
                            {
                                result = entityRoot;
                                num2 = sqrMagnitude;
                            }
                        }

                        checkedObjects.Add(entityRoot);
                    }

                    checkedObjectsField.SetValue(null, checkedObjects);
                    launchedObjectsField.SetValue(__instance, launchedObjects);

                    __result = result;

                    lastTraced = result;
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"{e}");
                }

                return false;
            }

            [HarmonyPostfix]
            public static void Postfix(ref GameObject __result)
            {
                Mod.infoLabel.debugInfoString = $"{lastTraced}";
                __result = lastTraced;
            }
        }

        [HarmonyPatch(typeof(PropulsionCannon))]
        [HarmonyPatch("IsAllowedToGrab")]
        public class Patch_PropulsionCannon_4 : MonoBehaviour
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }
    }
}
