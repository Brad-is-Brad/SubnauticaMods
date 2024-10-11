using FMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace SpookySubnautica.Handlers
{
    internal class StormHandler
    {
        // Lightning objects
        static LineRenderer lightningLineRenderer_Shocker = null;
        static LineRenderer lightningLineRenderer_Propulsion = null;
        static List<Sound> thunderSounds = new List<Sound>();
        static string thunderSoundBus = "bus:/master/SFX_for_pause/PDA_pause/all/SFX/creatures surface";

        // Lightning settings
        static float minTimeBetweenStrikes = 2f;
        static float maxTimeBetweenStrikes = 15f;
        static float lastStrikeTime = 0;
        static float nextStrikeTime = 0;
        static Color lightningBrightColor = Color.white;
        static Color lightningDimColor = new Color(0.3f, 0.3f, 0.3f);
        static float maxLightningVisisbleTime = 1f;
        static float lightningBlinkTime = 0.25f;

        static float maxDamageDistance = 50f;
        static float maxDamage = 50f;

        static float lightningHeight = 310f;
        static float maxLightningDistance = 800f;
        static float maxOffset = 5f;
        static Vector3 tempOffset;
        static float numSquiggles = 100f;
        static float branchChance = 0.2f;
        static int numSquigglesInBranch = 10;
        static int minSquigglesSinceLastBranch = (int)Math.Round(numSquiggles / 5f);
        static Vector3[] branchDirections = new Vector3[] {
            new Vector3(0f, 0f, 20f),
            new Vector3(20f, 0f, 20f),
            new Vector3(20f, 0f, 0f),
            new Vector3(20f, 0f, -20f),
            new Vector3(0f, 0f, -20f),
            new Vector3(-20f, 0f, -20f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-20f, 0f, 20f),
        };

        static ParticleSystem airSmokeParticleSystem;
        static float airSmokeHeight = 0f;

        static ParticleSystem cloudParticleSystem;
        static float cloudHeight = 300f;

        public static void Start()
        {
            LoadThunderSounds();
        }

        private static void GetParticleSystems()
        {
            if (airSmokeParticleSystem != null) { return; }
            if (CrashedShipExploder.main == null) { return; }

            Plugin.Logger.LogInfo($"Starting GetParticleSystems...");

            ParticleSystem[] foundParticleSystems =
                CrashedShipExploder.main.gameObject.GetAllComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem particleSystem in foundParticleSystems)
            {
                if (airSmokeParticleSystem != null && cloudParticleSystem != null) break;

                // TODO: xGlow for ominous red glow?
                if (particleSystem.name.Equals("xSmk") && airSmokeParticleSystem == null)
                {
                    Plugin.Logger.LogInfo($"Starting SetAirSmoke...");
                    airSmokeParticleSystem = SetAirSmoke(particleSystem);
                    Plugin.Logger.LogInfo($"Finished SetAirSmoke...");
                }
                else if (particleSystem.name.Equals("xSmkColumn2") && cloudParticleSystem == null)
                {
                    Plugin.Logger.LogInfo($"Starting SetClouds...");
                    cloudParticleSystem = SetClouds(particleSystem);
                    Plugin.Logger.LogInfo($"Finished SetClouds...");
                }
            }
        }

        private static ParticleSystem SetAirSmoke(ParticleSystem originalParticleSystem)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(originalParticleSystem.gameObject);

            ParticleSystem airSmokeParticleSystem = gameObject.GetComponent<ParticleSystem>();

            MainModule mainModule = airSmokeParticleSystem.main;
            mainModule.startLifetimeMultiplier = 10;
            mainModule.startSpeedMultiplier = 0;
            mainModule.startSizeMultiplier = 16;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            mainModule.maxParticles = 2000;
            mainModule.prewarm = false;

            VelocityOverLifetimeModule velocity = airSmokeParticleSystem.velocityOverLifetime;
            velocity.speedModifier = 0;

            EmissionModule emission = airSmokeParticleSystem.emission;
            emission.rateOverTimeMultiplier = 500;

            ShapeModule shape = airSmokeParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 200;
            shape.rotation = new Vector3(-90, 0, 0);

            gameObject.transform.localEulerAngles = Vector3.zero;
            gameObject.transform.position = new Vector3(0f, airSmokeHeight, 0f);

            return airSmokeParticleSystem;
        }

        private static ParticleSystem SetClouds(ParticleSystem originalParticleSystem)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(originalParticleSystem.gameObject);

            ParticleSystem cloudParticleSystem = gameObject.GetComponent<ParticleSystem>();

            MainModule mainModule = cloudParticleSystem.main;
            mainModule.startLifetimeMultiplier = 10;
            mainModule.startSpeedMultiplier = 0;
            mainModule.startSizeMultiplier = 400;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            mainModule.maxParticles = 8000;
            mainModule.prewarm = false;

            VelocityOverLifetimeModule velocity = cloudParticleSystem.velocityOverLifetime;
            velocity.speedModifier = 0;

            EmissionModule emission = cloudParticleSystem.emission;
            emission.rateOverTimeMultiplier = 1000;

            ShapeModule shape = cloudParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1500;
            shape.rotation = new Vector3(-90, 0, 0);

            gameObject.transform.localEulerAngles = Vector3.zero;
            gameObject.transform.position = new Vector3(0f, cloudHeight, 0f);

            return cloudParticleSystem;
        }

        private static void GetElectricityMaterial()
        {
            lightningLineRenderer_Shocker = GetElectricityMaterialFromShocker();
            lightningLineRenderer_Propulsion = GetElectricityMaterialFromPropulsionCannon();
        }

        private static LineRenderer GetElectricityMaterialFromShocker()
        {
            if (!Mod.cachedPrefabs.ContainsKey(TechType.Shocker)) return null;

            GameObject gameObject = CraftData.InstantiateFromPrefab(
                Mod.cachedPrefabs[TechType.Shocker],
                TechType.Shocker
            );

            VFXShockerElecLine elecLine = gameObject.GetComponent<VFXShockerElec>().ringElecLine;
            Material material = elecLine.gameObject.GetComponent<Renderer>().material;
            Plugin.Logger.LogInfo($"Shocker Material: {material}");

            GameObject lightningGameObject = new GameObject();
            LineRenderer lineRenderer = lightningGameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 2f;
            lineRenderer.endWidth = 2f;
            lineRenderer.sharedMaterial = material;
            lineRenderer.enabled = false;

            UnityEngine.Object.Destroy(gameObject);

            return lineRenderer;
        }

        private static LineRenderer GetElectricityMaterialFromPropulsionCannon()
        {
            if (!Mod.cachedPrefabs.ContainsKey(TechType.PropulsionCannon)) return null;

            GameObject gameObject = CraftData.InstantiateFromPrefab(
                Mod.cachedPrefabs[TechType.PropulsionCannon],
                TechType.PropulsionCannon
            );
            PropulsionCannon propulsionCannon = gameObject.GetComponent<PropulsionCannon>();
            VFXElectricLine[] elecLine = propulsionCannon.fxBeam.GetComponentsInChildren<VFXElectricLine>(includeInactive: true);
            Material material = elecLine[0].gameObject.GetComponent<Renderer>().material;
            Plugin.Logger.LogInfo($"PropulsionCannon Material: {material}");

            GameObject lightningGameObject = new GameObject();
            LineRenderer lineRenderer = lightningGameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 2f;
            lineRenderer.endWidth = 2f;
            lineRenderer.sharedMaterial = material;
            lineRenderer.enabled = false;

            UnityEngine.Object.Destroy(gameObject);

            return lineRenderer;
        }

        public static void LightningStrike()
        {
            // Make sure materials have loaded
            if (lightningLineRenderer_Shocker == null) { return; }
            if (lightningLineRenderer_Propulsion == null) { return; }

            if (
                // Don't strike if game hasn't fully loaded yet
                Player.main == null
                || Camera.main == null
                || MainCameraControl.main == null
                || DamageFX.main == null

                // Don't strike if player / camera is below water
                || Camera.main.transform.position.y < -2
            ) { return; }

            Vector3 playerPosition = Camera.main.transform.position;
            Vector3 strikeLocation = new Vector3(
                UnityEngine.Random.Range(playerPosition.x - maxLightningDistance, playerPosition.x + maxLightningDistance),
                0f,
                UnityEngine.Random.Range(playerPosition.z - maxLightningDistance, playerPosition.z + maxLightningDistance)
            );

            RaycastHit[] hits = Physics.RaycastAll(strikeLocation, Vector3.up, lightningHeight);
            if (hits.Length > 0) { return; }

            int branchDirectionOffset = 0;
            int squigglesSinceLastBranch = 0;

            List<Vector3> positions = new List<Vector3>();
            Vector3 lastPosition = new Vector3(strikeLocation.x, 0f, strikeLocation.z);
            for (float i = 1; i <= numSquiggles; i++)
            {
                positions.Add(lastPosition);
                lastPosition = new Vector3(
                    lastPosition.x + UnityEngine.Random.Range(-maxOffset, maxOffset),
                    lightningHeight * (i / numSquiggles),
                    lastPosition.z + UnityEngine.Random.Range(-maxOffset, maxOffset)
                );

                if (
                    i > numSquigglesInBranch + 1
                    && Mod.random.NextDouble() < branchChance
                    && squigglesSinceLastBranch >= minSquigglesSinceLastBranch
                )
                {
                    squigglesSinceLastBranch = 0;

                    List<Vector3> branchPositions = new List<Vector3>();
                    Vector3 branchPosition = positions.Last();
                    branchPositions.Add(branchPosition);

                    for (int j = 1; j <= numSquigglesInBranch; j++)
                    {
                        tempOffset = j == 1 ? branchDirections[branchDirectionOffset] : Vector3.zero;
                        branchPosition = new Vector3(
                            branchPosition.x + UnityEngine.Random.Range(-maxOffset, maxOffset) + tempOffset.x,
                            lightningHeight * ((i - j - 4) / numSquiggles),
                            branchPosition.z + UnityEngine.Random.Range(-maxOffset, maxOffset) + tempOffset.z
                        );
                        branchPositions.Add(branchPosition);
                        positions.Add(branchPosition);
                    }

                    // Return back up the same path
                    for (int j = numSquigglesInBranch - 1; j >= 0; j--)
                    {
                        positions.Add(branchPositions[j]);
                    }

                    branchDirectionOffset++;
                    if (branchDirectionOffset >= branchDirections.Length) { branchDirectionOffset = 0; }
                }
                else
                {
                    squigglesSinceLastBranch++;
                }
            }
            positions.Add(lastPosition);

            lightningLineRenderer_Shocker.positionCount = positions.Count;
            lightningLineRenderer_Shocker.SetPositions(positions.ToArray());
            lightningLineRenderer_Shocker.enabled = true;

            lightningLineRenderer_Propulsion.positionCount = positions.Count;
            lightningLineRenderer_Propulsion.SetPositions(positions.ToArray());
            lightningLineRenderer_Propulsion.enabled = true;

            lightningLineRenderer_Shocker.gameObject.transform.position = strikeLocation;
            lightningLineRenderer_Propulsion.gameObject.transform.position = strikeLocation;

            PlayThunderSound(strikeLocation);

            float strikeDistance = Vector3.Distance(strikeLocation, Camera.main.transform.position);
            if (strikeDistance <= maxDamageDistance)
            {
                LiveMixin playerLiveMixin = Player.main.GetComponent<LiveMixin>();
                if (playerLiveMixin != null)
                {
                    try
                    {
                        playerLiveMixin.TakeDamage(
                            (1f - (strikeDistance / maxDamageDistance)) * maxDamage,
                            type: DamageType.Electrical
                        );
                    }
                    catch (Exception e)
                    {
                        Plugin.Logger.LogInfo($"LightningStrike exception: {e}");
                    }
                    
                }
            }

            lastStrikeTime = Time.time;
            nextStrikeTime = Time.time + UnityEngine.Random.Range(minTimeBetweenStrikes, maxTimeBetweenStrikes);
        }

        static void PlayThunderSound(Vector3 position)
        {
            Mod.PlaySound(thunderSounds[Mod.random.Next(thunderSounds.Count)], thunderSoundBus, out Channel channel);
            ATTRIBUTES_3D attributes = FMODUnity.RuntimeUtils.To3DAttributes(position);
            channel.set3DAttributes(ref attributes.position, ref attributes.velocity);
        }

        public static void Update()
        {
            GetParticleSystems();

            if (lightningLineRenderer_Propulsion == null)
            {
                GetElectricityMaterial();
            }

            if (
                Time.time >= nextStrikeTime
                && DayNightHandler.isDay
            )
            {
                LightningStrike();
            }

            UpdateLightningBrightness();

            if (airSmokeParticleSystem != null)
            {
                airSmokeParticleSystem.gameObject.transform.position = new Vector3(
                    Camera.main.transform.position.x,
                    airSmokeHeight,
                    Camera.main.transform.position.z
                );
            }

            if (cloudParticleSystem != null)
            {
                cloudParticleSystem.gameObject.transform.position = new Vector3(
                    Camera.main.transform.position.x,
                    cloudHeight,
                    Camera.main.transform.position.z
                );

                if (DayNightHandler.isDay)
                {
                    // Day: storm!
                    cloudParticleSystem.Play();
                }
                else
                {
                    // Night: no storm!
                    cloudParticleSystem.Stop();
                }
            }
        }

        static void UpdateLightningBrightness()
        {
            if (lightningLineRenderer_Shocker == null) { return; }
            if (lightningLineRenderer_Propulsion == null) { return; }

            if (Time.time - lastStrikeTime > maxLightningVisisbleTime)
            {
                lightningLineRenderer_Shocker.enabled = false;
                lightningLineRenderer_Propulsion.enabled = false;
            }
            else if (Math.Floor((Time.time - lastStrikeTime) / maxLightningVisisbleTime / lightningBlinkTime) % 2 == 1)
            {
                lightningLineRenderer_Shocker.startColor = lightningDimColor;
                lightningLineRenderer_Shocker.endColor = lightningDimColor;
                lightningLineRenderer_Propulsion.startColor = lightningDimColor;
                lightningLineRenderer_Propulsion.endColor = lightningDimColor;
            }
            else if (Math.Floor((Time.time - lastStrikeTime) / maxLightningVisisbleTime / lightningBlinkTime) % 2 == 0)
            {
                lightningLineRenderer_Shocker.startColor = lightningBrightColor;
                lightningLineRenderer_Shocker.endColor = lightningBrightColor;
                lightningLineRenderer_Propulsion.startColor = lightningBrightColor;
                lightningLineRenderer_Propulsion.endColor = lightningBrightColor;
            }
        }

        private static void LoadThunderSounds()
        {
            thunderSounds.Add(Mod.LoadSound("Thunder.ogg", MODE._3D, thunderSoundBus));
            thunderSounds[0].set3DMinMaxDistance(100f, 1000f);

            thunderSounds.Add(Mod.LoadSound("Thunder2.ogg", MODE._3D, thunderSoundBus));
            thunderSounds[1].set3DMinMaxDistance(100f, 1000f);
        }
    }
}
