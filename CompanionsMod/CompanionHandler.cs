using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static CompanionsMod.uGUI_CompanionTab;

namespace CompanionsMod
{
    internal class CompanionHandler
    {
        public static CompanionHandler instance = null;
        public Companion curCompanion;

        public bool awaitingAttackTarget = false;
        public bool awaitingCaptureTarget = false;

        public GameObject companionBeaconGameObject;
        public PingInstance companionBeaconPingInstance;

        public GameObject companionHealthBeaconGameObject;
        public PingInstance companionHealthBeaconPingInstance;

        public static uGUI_Pings my_uGUI_Pings;

        public ProgressBar healthBar;
        public static bool healthBarsAdded = true;

        public GameObject enemyBeaconGameObject;
        public PingInstance enemyBeaconPingInstance;

        ProgressBar enemyHealthBar;

        public static Dictionary<Vector3, StorageContainer> storageContainers = new Dictionary<Vector3, StorageContainer>();

        public CompanionHandler()
        {
            try
            {
                companionBeaconGameObject = new GameObject();
                companionBeaconPingInstance = companionBeaconGameObject.AddComponent<PingInstance>();
                companionBeaconPingInstance.origin = companionBeaconGameObject.transform;
                companionBeaconPingInstance.SetType(PingType.Sunbeam);
                companionBeaconPingInstance.SetLabel("Companion");
                companionBeaconPingInstance.SetVisible(false);
                companionBeaconPingInstance.minDist = 2f;

                companionHealthBeaconGameObject = new GameObject();
                companionHealthBeaconPingInstance = companionHealthBeaconGameObject.AddComponent<PingInstance>();
                companionHealthBeaconPingInstance.origin = companionHealthBeaconGameObject.transform;
                companionHealthBeaconPingInstance.SetType(PingType.Sunbeam);
                companionHealthBeaconPingInstance.SetLabel("");
                companionHealthBeaconPingInstance.SetVisible(false);
                companionHealthBeaconPingInstance.displayPingInManager = false;

                enemyBeaconGameObject = new GameObject();
                enemyBeaconPingInstance = enemyBeaconGameObject.AddComponent<PingInstance>();
                enemyBeaconPingInstance.origin = enemyBeaconGameObject.transform;
                enemyBeaconPingInstance.SetType(PingType.Sunbeam);
                enemyBeaconPingInstance.SetLabel("Target");
                enemyBeaconPingInstance.SetVisible(false);
                enemyBeaconPingInstance.minDist = 1f;
                enemyBeaconPingInstance.displayPingInManager = false;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogInfo($"CompanionHandler constructor: {e}");
            }
        }

        [HarmonyPatch(typeof(uGUI_Pings))]
        [HarmonyPatch("Awake")]
        internal class Patch_uGUI_Pings_Awake
        {
            [HarmonyPostfix]
            public static void Postfix(uGUI_Pings __instance)
            {
                my_uGUI_Pings = __instance;
            }
        }

        [HarmonyPatch(typeof(LiveMixin))]
        [HarmonyPatch("maxHealth", MethodType.Getter)]
        internal class Patch_LiveMixin_maxHealth
        {
            [HarmonyPrefix]
            public static bool Prefix(LiveMixin __instance, ref float __result)
            {
                Companion companion = __instance.GetComponent<Companion>();
                if (__instance.gameObject.GetComponent<Companion>())
                {
                    __result = companion.maxHealth;
                    return false;
                }

                return true;
            }
        }

        void TryAddHealthBars()
        {
            // This can't be done immediately in the Setup function, or trying to access pingInstance.Id throws an error
            if (!healthBarsAdded)
            {
                Plugin.Logger.LogInfo($"Adding health bars!");

                Dictionary<string, uGUI_Ping> pings = my_uGUI_Pings.pings;
                uGUI_Ping ping = pings[companionHealthBeaconPingInstance.Id];
                ping.icon.enabled = false;

                // Health bar
                healthBar =
                    new ProgressBar(
                        ping.icon.transform,
                        $"",
                        new Color(0.859f, 0.373f, 0.251f),
                        250f
                    )
                ;

                uGUI_Ping enemyPing = pings[enemyBeaconPingInstance.Id];
                enemyPing.icon.enabled = false;

                // Enemy health bar
                enemyHealthBar = new ProgressBar(
                    enemyPing.icon.transform,
                    "",
                    new Color(0.859f, 0.373f, 0.251f),
                    250f
                );

                healthBarsAdded = true;
            }
        }

        public void DoUpdate()
        {
            if (
                Player.main.GetPDA().isOpen
                || Player.main.cinematicModeActive
            )
            {
                awaitingAttackTarget = false;
                awaitingCaptureTarget = false;
            }

            TryAddHealthBars();

            if (curCompanion != null && curCompanion.GetCreature() != null)
            {
                if (curCompanion.GetCreature().liveMixin.health < curCompanion.GetCreature().liveMixin.maxHealth)
                {
                    companionHealthBeaconPingInstance.SetVisible(true);
                    companionHealthBeaconPingInstance.minDist = 1f;
                }
                else
                {
                    companionHealthBeaconPingInstance.SetVisible(false);
                }

                if (!curCompanion.GetCreature().liveMixin.IsAlive() && companionBeaconPingInstance != null)
                {
                    //companionBeaconPingInstance.SetLabel($"[{team}] {techType} [DEAD]");
                    //healthBar.Hide();
                }
            }

            if (healthBar != null && curCompanion != null && curCompanion.GetCreature() != null)
            {
                healthBar.SetProgress(
                    curCompanion.GetCreature().liveMixin.health,
                    curCompanion.GetCreature().liveMixin.maxHealth
                );
            }
            else
            {
                // hide it!
            }

            if (
                enemyHealthBar != null
                && curCompanion != null
                && curCompanion.attackTarget != null
                && curCompanion.attackTarget.GetComponentInChildren<LiveMixin>() != null
            )
            {
                enemyHealthBar.SetProgress(
                    curCompanion.attackTarget.GetComponentInChildren<LiveMixin>().health,
                    curCompanion.attackTarget.GetComponentInChildren<LiveMixin>().maxHealth
                );
            }
            else if (enemyHealthBar != null)
            {
                enemyHealthBar.SetProgress(0, 1);
            }

            TryCapture();

            if (curCompanion == null) {
                enemyBeaconPingInstance.SetVisible(false);
                if (awaitingAttackTarget) awaitingAttackTarget = false;
                return;
            }

            curCompanion.DoUpdate();

            TryChooseAttackTarget();
        }

        public void TryCapture()
        {
            if (awaitingCaptureTarget)
            {
                GameObject lookingAt = Mod.GetLookingAtCreature();

                if (
                    Input.GetMouseButtonDown(0)
                    && lookingAt != null
                    && (
                        curCompanion == null
                        || curCompanion.gameObject != lookingAt
                    )
                )
                {
                    Creature creature = lookingAt.GetComponent<Creature>();
                    WaterParkCreature waterParkCreature = creature.gameObject.GetComponent<WaterParkCreature>();

                    if (
                        creature != null
                        && creature.liveMixin != null
                        && (
                            Plugin.config.captureAnything
                            || creature.IsFriendlyTo(Player.main.gameObject)
                        )
                        && (
                            waterParkCreature == null
                            || !waterParkCreature.isInside
                        )
                    )
                    {
                        Plugin.Logger.LogInfo($"Capturing - lookingAt: {lookingAt}");
                        Companion companion = lookingAt.GetComponent<Companion>();
                        if (companion == null)
                        {
                            Plugin.Logger.LogInfo($"Capturing - companion was null: {companion}");
                            companion = lookingAt.AddComponent<Companion>();
                            Plugin.Logger.LogInfo($"Capturing - companion created: {companion}");
                            companion.Setup();
                        }

                        if (curCompanion != null)
                        {
                            curCompanion.Release();
                            //UnityEngine.Object.Destroy(curCompanion);
                        }
                        curCompanion = companion;
                        awaitingCaptureTarget = false;

                        uGUI_CompanionTab.instance.UpdateCompanionName();
                        CompanionHandler.instance.companionBeaconPingInstance.SetVisible(true);

                        /*Warper warper = companion.GetComponent<Warper>();
                        if (warper != null)
                        {
                            FieldInfo spawnerField = typeof(Warper)
                                .GetField("spawner", BindingFlags.NonPublic | BindingFlags.Instance);
                            WarperSpawner spawner = (WarperSpawner)spawnerField.GetValue(warper);

                        }*/

                        Plugin.Logger.LogInfo($"Capturing done!");
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    awaitingCaptureTarget = false;
                }
                else
                {
                    string targetName = "";
                    if (lookingAt != null)
                    {
                        Creature creature = lookingAt.GetComponent<Creature>();
                        targetName = lookingAt.name.Replace("(Clone)", "").Trim();
                        if (
                            creature != null
                            && creature.liveMixin != null
                        )
                        {
                            bool friendly = creature.IsFriendlyTo(Player.main.gameObject);
                            if (!friendly)
                            {
                                targetName += " (Wild)";
                            }

                            WaterParkCreature waterParkCreature =
                                creature.gameObject.GetComponent<WaterParkCreature>();
                            if (
                                waterParkCreature != null
                                && waterParkCreature.isInside
                            )
                            {
                                targetName += " (Tank)";
                            }
                        }
                    }

                    HandReticle.main.SetText(HandReticle.TextType.Hand, lookingAt ? $"Capture {targetName}" : "No target", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, "Cancel", translate: false, GameInput.Button.RightHand);
                    HandReticle.main.SetIcon(lookingAt ? HandReticle.IconType.Interact : HandReticle.IconType.None);
                }
            }
        }

        void TryChooseAttackTarget()
        {
            if (awaitingAttackTarget)
            {
                GameObject lookingAt = Mod.GetLookingAtCreature();

                if (Input.GetMouseButtonDown(0) && lookingAt != null && lookingAt != curCompanion.gameObject)
                {
                    curCompanion.SetAttackTarget(lookingAt);
                    awaitingAttackTarget = false;
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    awaitingAttackTarget = false;
                }
                else
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, lookingAt ? $"Attack {lookingAt.name.Replace("(Clone)", "").Trim()}" : "No target", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, "Cancel", translate: false, GameInput.Button.RightHand);
                    HandReticle.main.SetIcon(lookingAt ? HandReticle.IconType.Interact : HandReticle.IconType.None);
                }
            }
        }

        public void Release()
        {
            curCompanion.Release();
            //UnityEngine.Object.Destroy(curCompanion);
            curCompanion = null;
            companionBeaconPingInstance.SetVisible(false);
            companionHealthBeaconPingInstance.SetVisible(false);
        }

        [HarmonyPatch(typeof(Creature))]
        [HarmonyPatch("ChooseBestAction")]
        internal class Patch_Creature_ChooseBestAction
        {
            [HarmonyPrefix]
            public static bool Prefix(Creature __instance, ref CreatureAction __result)
            {
                if (instance != null && instance.curCompanion != null && instance.curCompanion.GetCreature() == __instance)
                {
                    __result = instance.curCompanion;
                    return false;
                }

                return true;
            }
        }

        // TODO: dunno that we need this? override different function? override more classes?
        [HarmonyPatch(typeof(AggressiveWhenSeeTarget))]
        [HarmonyPatch("GetAggressionTarget")]
        internal class Patch_AggressiveWhenSeeTarget_GetAggressionTarget
        {
            [HarmonyPrefix]
            public static bool Prefix(AggressiveWhenSeeTarget __instance, ref GameObject __result)
            {
                if (instance != null && instance.curCompanion != null && instance.curCompanion.gameObject == __instance.gameObject)
                {
                    __result = Player.main.gameObject;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(MeleeAttack))]
        [HarmonyPatch("OnTouch")]
        internal class Patch_MeleeAttack_OnTouch
        {
            [HarmonyPrefix]
            public static bool Prefix(MeleeAttack __instance, Collider collider)
            {
                try
                {
                    if (
                        instance != null
                        && instance.curCompanion != null
                        && instance.curCompanion.gameObject == __instance.gameObject
                    )
                    {
                        GameObject target = __instance.GetTarget(collider);

                        if (instance.curCompanion.gameObject == target)
                            return false;
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"MeleeAttack.OnTouch error: {e}");
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Warper))]
        [HarmonyPatch("WarpOut")]
        internal class Patch_Warper_WarpOut
        {
            [HarmonyPrefix]
            public static bool Prefix(Warper __instance)
            {
                if (instance.curCompanion != null && instance.curCompanion.gameObject == __instance.gameObject)
                {
                    // Prevent Warper from warping out
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(uGUI_PingEntry))]
        [HarmonyPatch("UpdateLabel")]
        public class Patch_uGUI_PingEntry_UpdateLabel : MonoBehaviour
        {
            [HarmonyPrefix]
            public static bool Prefix(uGUI_PingEntry __instance, PingType type, string name)
            {
                if (
                    instance != null
                    && (
                        __instance.id.Equals(instance.companionBeaconPingInstance._id)
                        || __instance.id.Equals(instance.companionHealthBeaconPingInstance._id)
                        || __instance.id.Equals(instance.enemyBeaconPingInstance._id)
                    )
                )
                {
                    // Set PDA ping label properly
                    __instance.label.text = name;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(uGUI_PingEntry))]
        [HarmonyPatch("SetIcon")]
        public class Patch_uGUI_PingEntry_SetIcon : MonoBehaviour
        {
            [HarmonyPrefix]
            public static bool Prefix(uGUI_PingEntry __instance, PingType type)
            {
                if (
                    instance != null
                    && (
                        __instance.id.Equals(instance.companionBeaconPingInstance._id)
                        || __instance.id.Equals(instance.companionHealthBeaconPingInstance._id)
                        || __instance.id.Equals(instance.enemyBeaconPingInstance._id)
                    )
                )
                {
                    // Set PDA ping icon to the Sunbeam icon
                    __instance.icon.SetForegroundSprite(
                        SpriteManager.Get(
                            SpriteManager.Group.Pings,
                            PingManager.sCachedPingTypeStrings.Get(PingType.Sunbeam)
                        )
                    );

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(StorageContainer))]
        [HarmonyPatch("Awake")]
        internal class Patch_StorageContainer_Awake
        {
            [HarmonyPostfix]
            public static void Postfix(StorageContainer __instance)
            {
                string prefabName = __instance.gameObject.GetComponentInParent<PrefabIdentifier>().name;
                if (prefabName.Contains("luggage") && __instance.gameObject.transform.position != Vector3.zero)
                {
                    Plugin.Logger.LogInfo($"Found luggage bag: {__instance.gameObject.transform.position}");
                    storageContainers.Add(__instance.gameObject.transform.position, __instance);
                }
            }
        }

        public static void Quit()
        {
            instance = null;
        }
    }
}
