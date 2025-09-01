using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static VFXParticlesPool;

namespace CompanionsMod
{
    [JsonObject(MemberSerialization.OptIn)]
    class Companion : CreatureAction
    {
        [JsonProperty]
        string companionId;

        public const string storageLabelPrefix = "companion id ";

        Creature myCreature;
        SwimRandom mySwimRandom;
        public GameObject attackTarget;
        public MeleeAttack myMeleeAttack;

        [JsonProperty]
        TechType techType;

        [JsonProperty]
        public string companionName;

        float swimInterval = 1f;
        float timeNextSwim;

        [JsonProperty]
        public bool shouldFollow = true;

        [JsonProperty]
        Vector3 stayPosition = Vector3.zero;

        [JsonProperty]
        float maxRendererSize = 0;

        float warpDistance = 200;

        [JsonProperty]
        public int experiencePoints = 0;

        [JsonProperty]
        Vector3 lastPosition = Vector3.zero;

        [JsonProperty]
        Vector3 inventoryContainerPosition = Vector3.zero;

        InfectedMixin infectedMixin;

        [JsonProperty]
        float infectedAmount = 0;

        public int curLevel = 0;
        float curScale = 1;
        public float minScale = 1;
        public float maxScale = 1;
        public float startBiteDamage = 1;
        public float startMaxHealth = 1;
        public float maxHealth = 1;

        public float startSwimVelocity = 1;
        public float startAttackSwimVelocity = 1;
        const float minSwimVelocity = 3f;
        const float minAttackSwimVelocity = 5f;

        public ItemsContainer itemsContainer;

        const float healIntervalSeconds = 5f;
        const float healPercentagePerInterval = 1f;
        float lastDamageHealCooldown = 30f;
        float lastDamagedTime = 0f;
        float lastHealedTime = 0f;

        [JsonProperty]
        float lastHealth = 0f;

        // TODO: test capturing multiple creatures inventory

        // TODO: wander to follow distance, swim back to half distance
        // TODO: if they eat the creature, they get no xp

        // TODO: make releasing make companion no longer global entity?

        // FEATURE: make companion inventory scale with level? with creature size?
        // FEATURE: play bite noise when attack (non-attacking creatures)?
        // FEATURE: make scale stick when placed in or taken out of aquarium?
        // FEATURE: ride them as mounts??
        // FEATURE: pet the creature
        // FEATURE: more than one companion
        // FEATURE: creature collects resources?
        // FEATURE: creature attacks when player is attacked?

        public void Setup(string loadCompanionId = null)
        {
            companionId = loadCompanionId;
            if (loadCompanionId == null)
            {
                companionId = Guid.NewGuid().ToString();
            }

            Plugin.Logger.LogInfo($"Companion - Setup - id: {loadCompanionId}");

            techType = CraftData.GetTechType(gameObject);

            myCreature = gameObject.GetComponent<Creature>();

            Plugin.Logger.LogInfo($"Capturing - friend?: {myCreature.IsFriendlyTo(Player.main.gameObject)}");

            mySwimRandom = gameObject.GetComponent<SwimRandom>();
            if (mySwimRandom == null)
            {
                Plugin.Logger.LogInfo($"Companion {gameObject.name} has no SwimRandom, creating one!");
                mySwimRandom = gameObject.AddComponent<SwimRandom>();
            }

            swimBehaviour = gameObject.GetComponent<SwimBehaviour>();

            AggressiveWhenSeeTarget awst = myCreature.GetComponent<AggressiveWhenSeeTarget>();
            if (awst == null)
            {
                Plugin.Logger.LogInfo($"Companion {gameObject.name} has no AggressiveWhenSeeTarget, creating one!");
                awst = myCreature.gameObject.AddComponent<AggressiveWhenSeeTarget>();
            }

            LastTarget lastTarget = myCreature.gameObject.GetComponent<LastTarget>();
            if (lastTarget == null)
            {
                lastTarget = myCreature.gameObject.AddComponent<LastTarget>();
            }

            AttackLastTarget alt = myCreature.GetComponent<AttackLastTarget>();
            if (alt == null)
            {
                Plugin.Logger.LogInfo($"Companion {gameObject.name} has no AttackLastTarget, creating one!");
                alt = myCreature.gameObject.AddComponent<AttackLastTarget>();
                alt.lastTarget = lastTarget;
            }

            myMeleeAttack = gameObject.GetComponent<MeleeAttack>();
            if (myMeleeAttack == null)
            {
                Plugin.Logger.LogInfo($"Companion {gameObject.name} has no MeleeAttack, creating one!");

                GameObject collisionGameObject = new GameObject();
                collisionGameObject.transform.position = gameObject.transform.position;
                collisionGameObject.transform.parent = gameObject.transform;

                Rigidbody rigidbody = collisionGameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;

                SphereCollider sphereCollider = collisionGameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.enabled = true;
                sphereCollider.radius = 10f;

                myMeleeAttack = gameObject.AddComponent<MeleeAttack>();
                myMeleeAttack.creature = myCreature;
                myMeleeAttack.mouth = collisionGameObject;
                myMeleeAttack.lastTarget = lastTarget;
                myMeleeAttack.liveMixin = myCreature.liveMixin;
                myMeleeAttack.enabled = true;
                // TODO: add attack sound here?

                BehaviourUpdateUtils.Register(myMeleeAttack);
            }

            experiencePoints = 0;

            Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
            float normalSize = renderer.bounds.size.magnitude;

            if (normalSize < 2)
            {
                minScale = 0.5f;
                maxScale = 5f;
            }
            else if (normalSize < 30)
            {
                minScale = 0.25f;
                maxScale = 3f;
            }
            else
            {
                minScale = 0.1f;
                maxScale = 1.5f;
            }

            startBiteDamage = myMeleeAttack.biteDamage;
            startMaxHealth = myCreature.liveMixin.data.maxHealth;
            maxHealth = startMaxHealth;
            Plugin.Logger.LogInfo($"Set {gameObject} maxHealth Setup(): {maxHealth}");
            startSwimVelocity = myCreature.GetComponent<SwimRandom>().swimVelocity;
            startAttackSwimVelocity = myCreature.GetComponent<AttackLastTarget>().swimVelocity;

            // Prevent companion from despawning
            LargeWorldEntity largeWorldEntity = gameObject.GetComponent<LargeWorldEntity>();
            largeWorldEntity.cellLevel = LargeWorldEntity.CellLevel.Global;
            if ((bool)LargeWorldStreamer.main && LargeWorldStreamer.main.cellManager != null)
            {
                LargeWorldStreamer.main.cellManager.RegisterEntity(largeWorldEntity);
            }

            creature.friendlyToPlayer = true;

            // Peeper: 1m
            // SandShark: 10m
            // Reaper: 80m

            UpdateStats();

            bool foundInventory = false;
            float maxX = 0;

            foreach (KeyValuePair<Vector3, StorageContainer> entry in CompanionHandler.storageContainers)
            {
                if (inventoryContainerPosition != Vector3.zero && inventoryContainerPosition == entry.Key)
                {
                    // Found companion inventory!
                    foundInventory = true;
                    itemsContainer = entry.Value.container;
                    Plugin.Logger.LogInfo($"Found companion inventory: {inventoryContainerPosition}");
                }

                if (entry.Key.x > maxX) maxX = entry.Key.x;
            }

            if (!foundInventory)
            {
                GameObject bag = Instantiate(
                    PrefabHandler.cachedPrefabs[TechType.LuggageBag]
                );
                bag.SetActive(value: true);
                LargeWorldEntity.Register(bag);
                CrafterLogic.NotifyCraftEnd(bag, techType);
                bag.SendMessage("StartConstruction", SendMessageOptions.DontRequireReceiver);

                LargeWorldEntity bagLargeWorldEntity = bag.GetComponent<LargeWorldEntity>();
                bagLargeWorldEntity.cellLevel = LargeWorldEntity.CellLevel.Global;

                bag.transform.position = new Vector3(maxX + 1, -2000, 0);
                StorageContainer sc = bag.GetComponentInChildren<StorageContainer>();

                inventoryContainerPosition = bag.transform.position;
                itemsContainer = sc.container;
                Plugin.Logger.LogInfo($"Created companion inventory: {inventoryContainerPosition}");
                CompanionHandler.storageContainers.Add(inventoryContainerPosition, sc);
            }

            infectedMixin = gameObject.GetComponent<InfectedMixin>();
            if (infectedMixin != null)
            {
                infectedMixin.SetInfectedAmount(infectedAmount);
            }

            InfectedMixin component = gameObject.GetComponent<InfectedMixin>();
            if ((bool)component && component.IsInfected() && UnityEngine.Random.value < 0.2f)
            {
                component.SetInfectedAmount(1f);
            }

            if (companionId == null) companionName = GetCleanTypeName();

            Plugin.Logger.LogInfo($"Companion {companionName} Setup!");
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (
                CompanionHandler.instance.curCompanion == this
                && attackTarget != null
                && collision.collider.gameObject == attackTarget)
            {
                Plugin.Logger.LogInfo($"BITE!!! - {collision.collider.gameObject}");
                myMeleeAttack.OnTouch(collision.collider);
            }
        }

        public Creature GetCreature()
        {
            return myCreature;
        }

        public string GetCleanTypeName()
        {
            if (gameObject != null)
            {
                return gameObject.name.Split('(')[0].Trim();
            }

            return "";
        }

        public void Stay()
        {
            stayPosition = Camera.main.transform.position;
            shouldFollow = false;
        }

        public void Follow()
        {
            shouldFollow = true;
            SetAttackTarget(null);
        }

        public void Release()
        {
            shouldFollow = true;
            SetAttackTarget(null);
        }

        public GameObject GetAttackTarget()
        {
            return attackTarget;
        }

        public void SetAttackTarget(GameObject newTarget)
        {
            attackTarget = newTarget;
            AggressiveWhenSeeTarget awst = myCreature.GetComponent<AggressiveWhenSeeTarget>();
            if (awst.sightedSound != null && !awst.sightedSound.GetIsPlaying())
            {
                awst.sightedSound.StartEvent();
            }
        }

        public void DoUpdate()
        {
            if (gameObject == null) { Plugin.Logger.LogInfo($"Companion.DoUpdate - gameObject was null!"); return; }
            if (myCreature == null) { Plugin.Logger.LogInfo($"Companion.DoUpdate - MyCreature was null!"); return; }
            if (mySwimRandom == null) { Plugin.Logger.LogInfo($"Companion.DoUpdate - mySwimRandom was null!"); return; }
            if (CompanionHandler.instance.curCompanion != this) { Plugin.Logger.LogInfo($"Companion.DoUpdate - Not current companion!"); return; }

            // Give lots of XP
            /*if (Input.GetKey(KeyCode.P))
            {
                int amountGiven = (int)Math.Max(1, Math.Floor(Math.Sqrt(experiencePoints) / 4f));
                Plugin.Logger.LogInfo($"Giving XP - {amountGiven}");
                GiveExperiencePoints(amountGiven);
            }*/

            CompanionHandler.instance.companionBeaconGameObject.transform.position = myCreature.gameObject.transform.position;
            CompanionHandler.instance.companionBeaconPingInstance.SetVisible(true);

            uGUI_Ping ping = CompanionHandler.my_uGUI_Pings.pings[CompanionHandler.instance.companionBeaconPingInstance.Id];
            bool showDistance = ping._distance >= 15;
            ping.distanceText.enabled = showDistance;
            ping.suffixText.enabled = showDistance;
            ping.icon.enabled = showDistance;
            ping.SetLabel("Companion");

            CompanionHandler.instance.companionHealthBeaconGameObject.transform.position = myCreature.gameObject.transform.position;

            if (attackTarget != null)
            {
                CompanionHandler.instance.enemyBeaconGameObject.transform.position = attackTarget.transform.position;
                CompanionHandler.instance.enemyBeaconPingInstance.SetVisible(true);
            }
            else
            {
                CompanionHandler.instance.enemyBeaconPingInstance.SetVisible(false);
            }

            if (myCreature.liveMixin.health < lastHealth)
            {
                lastDamagedTime = Time.time;
            }
            lastHealth = myCreature.liveMixin.health;

            if (Time.time - lastDamagedTime >= lastDamageHealCooldown
                && Time.time - lastHealedTime >= healIntervalSeconds
            )
            {
                myCreature.liveMixin.AddHealth(
                    maxHealth
                    * (healPercentagePerInterval / 100f)
                );

                lastHealedTime = Time.time;
            }

            lastPosition = myCreature.transform.position;

            if (infectedMixin == null)
            {
                infectedMixin = gameObject.GetComponent<InfectedMixin>();
            }

            if (infectedMixin != null)
            {
                infectedAmount = infectedMixin.infectedAmount;
            }

            if (!myCreature.liveMixin.IsAlive())
            {
                Release();
            }

            // This will prevent Gasopods from releasing gas pods
            myCreature.Scared.Add(-1);
        }

        public int CalculateLevel()
        {
            return (int)Math.Floor(Math.Sqrt(experiencePoints) / 4f);
        }

        private void GiveExperiencePoints(int amount)
        {
            experiencePoints += amount;

            int level = CalculateLevel();
            if (level != curLevel)
            {
                curLevel = level;

                UpdateStats();

                Plugin.Logger.LogInfo($"Level up! - Level: {level} - Scale: {curScale} ({minScale} - {maxScale}) - Attack: {startBiteDamage} -> {myMeleeAttack.biteDamage}");
            }
        }

        public void UpdateStats()
        {
            curScale = (curLevel * curLevel + curLevel) / (curLevel * curLevel + curLevel + 200f) * (maxScale - minScale) + minScale;

            myCreature.SetScale(curScale);
            maxRendererSize = 0f;

            float statScale = (curScale + (float)Math.Pow(curLevel, 0.75f));

            myMeleeAttack.biteDamage = startBiteDamage * statScale;

            LiveMixin liveMixin = myCreature.liveMixin;
            float healthRatio = liveMixin.health / maxHealth;

            // TODO: this is doubling max health
            maxHealth = startMaxHealth * statScale;
            Plugin.Logger.LogInfo($"Set {gameObject} maxHealth UpdateStats: {maxHealth}");
            liveMixin.health = maxHealth * healthRatio;
        }

        public override void Perform(Creature creature, float time, float deltaTime)
        {
            try
            {
                if (CompanionHandler.instance.curCompanion != this)
                {
                    Plugin.Logger.LogInfo($"Companion.DoUpdate - Not current companion!");
                    return;
                }

                if (gameObject == null)
                {
                    Plugin.Logger.LogInfo($"Perform - gameObject was null!");
                    return;
                }

                Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    Plugin.Logger.LogInfo($"Perform - renderer was null!");
                    return;
                }

                float rendererSize = renderer.bounds.size.magnitude;
                if (rendererSize > maxRendererSize)
                {
                    maxRendererSize = rendererSize;
                }

                if (swimBehaviour == null)
                {
                    Plugin.Logger.LogInfo($"Perform - swimBehaviour was null!");
                    return;
                }

                string debugString = $"Companion: {companionName} - {gameObject.name}" +
                    $"\nLocation: {gameObject.transform.position}" +
                    $"\nHealth: {myCreature.liveMixin.health}/{maxHealth}" +
                    $"\nHunger: {myCreature.Hunger.Value}"
                ;

                // Update distance to camera
                Vector3 targetPosition = shouldFollow ? Camera.main.transform.position : stayPosition;
                float magnitude = (targetPosition - creature.transform.position).magnitude;
                if (magnitude > warpDistance * curScale && attackTarget == null)
                {
                    gameObject.transform.position =
                        targetPosition +
                        (creature.transform.position - targetPosition).normalized
                        * (120f * curScale)
                    ;

                    timeNextSwim = time + swimInterval;

                    return;
                }

                if (time > timeNextSwim)
                {
                    timeNextSwim = time + swimInterval;

                    if (attackTarget != null)
                    {
                        LiveMixin liveMixin = attackTarget.GetComponent<LiveMixin>();
                        if (liveMixin != null && !liveMixin.IsAlive())
                        {
                            int xpAmount = (int)Math.Ceiling(maxHealth);
                            Plugin.Logger.LogInfo($"Victory!");
                            GiveExperiencePoints(xpAmount);
                            attackTarget = null;
                            return;
                        };

                        // TODO: is this needed?
                        AggressiveWhenSeeTarget awst = myCreature.GetComponent<AggressiveWhenSeeTarget>();
                        myCreature.Hunger.Value = awst.hungerThreshold;

                        AttackLastTarget alt = myCreature.GetComponent<AttackLastTarget>();
                        myCreature.Aggression.Value = Mathf.Clamp01(alt.aggressionThreshold + 0.1f);
                        alt.lastTarget.SetTarget(attackTarget);

                        Vector3 position = attackTarget.transform.position;
                        Vector3 targetDirection = (!(attackTarget.GetComponent<Player>() != null))
                            ? (attackTarget.transform.position - creature.transform.position).normalized
                            : (-MainCamera.camera.transform.forward)
                        ;
                        swimBehaviour.Attack(
                            position,
                            targetDirection,
                            Math.Max(minAttackSwimVelocity, startAttackSwimVelocity * curScale)
                        );

                        debugString += $"\n";
                        debugString += $"\nTarget: {attackTarget.name} {position}";
                        debugString += $"\nHealth: {liveMixin.health} / {maxHealth}";
                        debugString += $"\n";
                    }
                    else
                    {
                        float leashDistance = Math.Min(Math.Max(5f, maxRendererSize * 2f), warpDistance * curScale);
                        if (magnitude > leashDistance)
                        {
                            swimBehaviour.SwimTo(
                                targetPosition,
                                Math.Max(minSwimVelocity, startSwimVelocity * curScale)
                            );

                            // Update swimming more frequently while out of range of player / stayPosition
                            timeNextSwim = time + swimInterval / 2f;

                            debugString += $"\nDistance: {magnitude} (SwimTo)";
                        }
                        else
                        {
                            mySwimRandom.timeNextSwim = 0;
                            mySwimRandom.Perform(myCreature, Time.time, Time.deltaTime);

                            debugString += $"\nDistance: {magnitude} (SwimRandom)";
                        }
                    }
                }
                else
                {
                    debugString += $"\nDistance: {magnitude}";
                }

                debugString += $"\nRender Size: {gameObject.GetComponentInChildren<Renderer>().bounds.size.magnitude}";
                debugString += $"\nMax Size: {maxRendererSize}";
                debugString += $"\nSwim Speed: {startSwimVelocity * curScale} ({startSwimVelocity})";
                debugString += $"\nAttack Speed: {startAttackSwimVelocity * curScale} ({startAttackSwimVelocity})";

                Mod.infoLabel.debugLeftString = debugString;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogInfo($"Companion perform e: {e}");
            }
        }
    }
}
