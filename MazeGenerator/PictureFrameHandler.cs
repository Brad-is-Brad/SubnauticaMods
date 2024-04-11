using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace MazeGeneratorMod
{
    internal class PictureFrameHandler
    {
        private static PictureFrame middleWallPictureFrame;
        private static PictureFrame rightWallPictureFrame;

        private static GameObject rightWallRotatorGameObject;

        private static Sign powerClickSign;
        private static Sign floodClickSign;
        private static Sign creaturesClickSign;
        private static Sign oxygenClickSign;
        private static Sign seedClickSign;
        private static Sign sizeClickSign;
        private static Sign timerClickSign;
        private static Sign generateClickSign;

        private static Sign powerLabelSign;
        private static Sign floodLabelSign;
        private static Sign creaturesLabelSign;
        private static Sign oxygenLabelSign;
        private static Sign seedLabelSign;
        private static Sign sizeLabelSign;
        private static Sign timerLabelSign;
        private static Sign generateLabelSign;

        private static PictureFrame powerPictureFrame;
        private static PictureFrame floodPictureFrame;
        private static PictureFrame creaturesPictureFrame;
        private static PictureFrame oxygenPictureFrame;
        private static PictureFrame seedPictureFrame;
        private static PictureFrame sizePictureFrame;
        private static PictureFrame timerPictureFrame;
        private static PictureFrame generatePictureFrame;

        private static Sign seedValueSign;
        private static uGUI_SignInput seedValueSignInput;

        private static Sign sizeValueSign;
        private static uGUI_SignInput sizeValueSignInput;

        private static Sign generateValueSign;
        private static uGUI_SignInput generateValueSignInput;

        private static GameObject finishWallRotatorGameObject;
        private static PictureFrame finishWallPictureFrame;
        private static Sign finishWarpClickSign;
        private static Sign finishWarpLabelSign;
        private static PictureFrame finishWarpPictureFrame;

        // This tracks whether there are settings changes
        //  that require a new maze generation to take effect
        private static bool regenerationRequired = false;

        private static readonly MethodInfo updateColorMethod = typeof(uGUI_SignInput).GetMethod("UpdateColor", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch(typeof(Sign))]
        [HarmonyPatch("OnHandHover")]
        internal class Patch_Sign_OnHandHover
        {
            [HarmonyPrefix]
            public static bool Prefix(Sign __instance, GUIHand hand)
            {
                if (__instance == powerClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Toggle Power", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == floodClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Toggle Flood", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == creaturesClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Toggle Creatures", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == oxygenClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Toggle Oxygen", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == seedClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Change Seed", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == sizeClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Change Size", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == timerClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Toggle Timer", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == generateClickSign)
                {
                    if (MazeHandler.MazeIsGenerating())
                    {
                        HandReticle.main.SetText(HandReticle.TextType.Hand, "Generating maze...", translate: false, GameInput.Button.LeftHand);
                        HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                        HandReticle.main.SetIcon(HandReticle.IconType.HandDeny);
                        return false;
                    }

                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Generate Maze", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                if (__instance == finishWarpClickSign)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "Warp to Start", translate: false, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, translate: false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Sign))]
        [HarmonyPatch("OnHandClick")]
        internal class Patch_Sign_OnHandClick
        {
            [HarmonyPrefix]
            public static bool Prefix(Sign __instance, GUIHand hand)
            {
                if (__instance == powerClickSign)
                {
                    PowerHandler.ToggleBasePower();
                    UpdateUI();
                    return false;
                }

                if (__instance == floodClickSign)
                {
                    FloodHandler.ToggleFloodBase();
                    UpdateUI();
                    return false;
                }

                if (__instance == creaturesClickSign)
                {
                    regenerationRequired = true;
                    CreatureHandler.ToggleCreatures();
                    UpdateUI();
                    return false;
                }

                if (__instance == oxygenClickSign)
                {
                    Mod.oxygenEnabled = !Mod.oxygenEnabled;
                    UpdateUI();
                    return false;
                }

                if (__instance == seedClickSign)
                {
                    int seedBefore = MazeHandler.randomSeed;
                    uGUI.main.userInput.RequestString(
                        "Set Randomness Seed",
                        "Set",
                        MazeHandler.randomSeed.ToString(),
                        10,
                        (string seed) =>
                        {
                            bool couldParse = int.TryParse(seed, out MazeHandler.randomSeed);

                            if (couldParse)
                            {
                                MazeHandler.seedIsRandom = false;
                                MazeHandler.random = new System.Random(MazeHandler.randomSeed);
                            } else
                            {
                                MazeHandler.seedIsRandom = true;
                                MazeHandler.random = new System.Random();
                            }

                            if (MazeHandler.randomSeed != seedBefore)
                            {
                                regenerationRequired = true;
                            }

                            UpdateUI();
                        }
                    );

                    return false;
                }

                if (__instance == sizeClickSign)
                {
                    Int3 sizeBefore = MazeHandler.mazeSize;
                    uGUI.main.userInput.RequestString(
                        "Set Maze Size",
                        "Set",
                        $"{MazeHandler.mazeSize.x},{MazeHandler.mazeSize.z}",
                        7,
                        (string sizeInputText) =>
                        {
                            string[] splitSizeInput = sizeInputText.Split(',');
                            if (splitSizeInput.Length != 2) { return; }

                            bool couldParseX = int.TryParse(splitSizeInput[0], out int sizeX);
                            bool couldParseZ = int.TryParse(splitSizeInput[1], out int sizeZ);

                            if (couldParseX && couldParseZ && sizeX > 0 && sizeZ > 0)
                            {
                                sizeX = System.Math.Max(sizeX, 2);
                                sizeZ = System.Math.Max(sizeZ, 2);

                                sizeX = System.Math.Min(sizeX, 50);
                                sizeZ = System.Math.Min(sizeZ, 50);

                                Int3 newMazeSize = new Int3(sizeX, 1, sizeZ);
                                if (sizeBefore != newMazeSize)
                                {
                                    regenerationRequired = true;
                                }
                                
                                MazeHandler.mazeSize = newMazeSize;
                                UpdateUI();
                            }
                        }
                    );


                    UpdateUI();
                    return false;
                }

                if (__instance == timerClickSign)
                {
                    Mod.mazeTimerLabel.ToggleTimerEnabled();
                    UpdateUI();
                    return false;
                }

                if (__instance == generateClickSign)
                {
                    if (MazeHandler.MazeIsGenerating())
                    {
                        return false;
                    }

                    regenerationRequired = false;

                    MazeHandler.DestroyMaze();
                    MazeHandler.GenerateMaze();
                    MazeHandler.SpawnMazeBasePieces();
                    UpdateUI();
                    return false;
                }

                if (__instance == finishWarpClickSign)
                {
                    WarpScreenFXController screenFX = MainCamera.camera.GetComponent<WarpScreenFXController>();
                    Mod.CreateSound("event:/creature/warper/portal_open", true);
                    screenFX.StartWarp();

                    System.Threading.Tasks.Task.Delay(500).ContinueWith(t => {
                        Player.main.SetPosition(MazeHandler.basePosition + new Vector3(5f, 0, 5f));
                        Mod.CreateSound("event:/creature/warper/portal_close", true);
                        screenFX.StopWarp();
                    });

                    return false;
                }

                return true;
            }
        }

        public static void SpawnMazeSettingsPictureFrames()
        {
            DestroyMazeSettingsPictureFrames();

            Vector3 giantPictureFrameOffset = new Vector3(0.6f, 0f, 5f);
            Quaternion rotate90 = Quaternion.Euler(0f, 90f, 0f);
            Vector3 giantPictureFrameScale = new Vector3(2f, 2f, 2f);

            Vector3 clickSignScale = new Vector3(1.85f, 1.7f, 2f);

            Vector3 upperLeftPictureFramePos = new Vector3(0.615f, 0.34f, 4.375f);
            Vector3 upperLeftClickSignPos = new Vector3(0.65f, 0.34f, 4.375f);
            Vector3 upperLeftLabelSignPos = new Vector3(0.65f, 0.575f, 4.375f);

            Vector3 upperRightPictureFramePos = new Vector3(0.615f, 0.34f, 5.625f);
            Vector3 upperRightClickSignPos = new Vector3(0.65f, 0.34f, 5.625f);
            Vector3 upperRightLabelSignPos = new Vector3(0.65f, 0.575f, 5.625f);

            Vector3 lowerLeftPictureFramePos = new Vector3(0.615f, -0.34f, 4.375f);
            Vector3 lowerLeftClickSignPos = new Vector3(0.65f, -0.34f, 4.375f);
            Vector3 lowerLeftLabelSignPos = new Vector3(0.65f, -0.075f, 4.375f);

            Vector3 lowerRightPictureFramePos = new Vector3(0.615f, -0.34f, 5.625f);
            Vector3 lowerRightClickSignPos = new Vector3(0.65f, -0.34f, 5.625f);
            Vector3 lowerRightLabelSignPos = new Vector3(0.65f, -0.075f, 5.625f);

            // ---------- Middle wall ----------

            // Visible giant picture frame
            middleWallPictureFrame = SpawnPictureFrame(
                giantPictureFrameOffset,
                rotate90,
                giantPictureFrameScale,
                false,
                false
            );

            // ---- Power ----

            // Power toggle background picture
            powerPictureFrame = SpawnPictureFrame(
                upperLeftPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );

            // Power toggle click area
            powerClickSign = SpawnSign(
                upperLeftClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );

            // Power toggle label
            powerLabelSign = SpawnSign(
                upperLeftLabelSignPos,
                rotate90,
                clickSignScale,
                "Power",
                false
            );

            // ---- Flood ----

            // Flood toggle background picture
            floodPictureFrame = SpawnPictureFrame(
                upperRightPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );

            // Flood toggle click area
            floodClickSign = SpawnSign(
                upperRightClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );

            // Flood toggle label
            floodLabelSign = SpawnSign(
                upperRightLabelSignPos,
                rotate90,
                clickSignScale,
                "Flood",
                false
            );

            // ---- Creatures ----

            // Creatures toggle background picture
            creaturesPictureFrame = SpawnPictureFrame(
                lowerLeftPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );

            // Creatures toggle click area
            creaturesClickSign = SpawnSign(
                lowerLeftClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );

            // Creatures toggle label
            creaturesLabelSign = SpawnSign(
                lowerLeftLabelSignPos,
                rotate90,
                clickSignScale,
                "Creatures",
                false
            );

            // ---- Oxygen ----

            // Oxygen toggle background picture
            oxygenPictureFrame = SpawnPictureFrame(
                lowerRightPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );

            // Oxygen toggle click area
            oxygenClickSign = SpawnSign(
                lowerRightClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );

            // Oxygen toggle label
            oxygenLabelSign = SpawnSign(
                lowerRightLabelSignPos,
                rotate90,
                clickSignScale,
                "Oxygen",
                false
            );

            // ---------- Right wall ----------

            // Create a new empty game object to handle rotating the other screens cleanly
            rightWallRotatorGameObject = new GameObject();
            Transform rightWallRotator = rightWallRotatorGameObject.transform;
            rightWallRotator.position = MazeHandler.basePosition + new Vector3(5f, 0f, 5f);

            // Visible giant picture frame
            rightWallPictureFrame = SpawnPictureFrame(
                giantPictureFrameOffset,
                rotate90,
                giantPictureFrameScale,
                false,
                false
            );
            rightWallPictureFrame.transform.parent = rightWallRotator;

            // ---- Seed ----

            // Seed background picture
            seedPictureFrame = SpawnPictureFrame(
                upperLeftPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );
            seedPictureFrame.transform.parent = rightWallRotator;

            // Seed click area
            seedClickSign = SpawnSign(
                upperLeftClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );
            seedClickSign.transform.parent = rightWallRotator;

            // Seed label
            seedLabelSign = SpawnSign(
                upperLeftLabelSignPos,
                rotate90,
                clickSignScale,
                "Seed",
                false
            );
            seedLabelSign.transform.parent = rightWallRotator;

            seedValueSign = SpawnSign(
                upperLeftLabelSignPos + new Vector3(0, -0.3f, 0),
                rotate90,
                clickSignScale * 0.75f,
                "...",
                false
            );
            seedValueSign.transform.parent = rightWallRotator;

            seedValueSignInput = seedValueSign.GetComponentInChildren<uGUI_SignInput>();
            seedValueSignInput.colors = new Color[1] { Color.white };
            seedValueSignInput.colorIndex = 0;
            seedValueSign.colorIndex = 0;

            updateColorMethod.Invoke(seedValueSignInput, new object[] { });

            // ---- Size ----

            // Size background picture
            sizePictureFrame = SpawnPictureFrame(
                upperRightPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );
            sizePictureFrame.transform.parent = rightWallRotator;

            // Size click area
            sizeClickSign = SpawnSign(
                upperRightClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );
            sizeClickSign.transform.parent = rightWallRotator;

            // Size label
            sizeLabelSign = SpawnSign(
                upperRightLabelSignPos,
                rotate90,
                clickSignScale,
                "Size",
                false
            );
            sizeLabelSign.transform.parent = rightWallRotator;

            sizeValueSign = SpawnSign(
                upperRightLabelSignPos + new Vector3(0, -0.3f, 0),
                rotate90,
                clickSignScale * 0.75f,
                "...",
                false
            );
            sizeValueSign.transform.parent = rightWallRotator;

            sizeValueSignInput = sizeValueSign.GetComponentInChildren<uGUI_SignInput>();
            sizeValueSignInput.colors = new Color[1] { Color.white };
            sizeValueSignInput.colorIndex = 0;
            sizeValueSign.colorIndex = 0;

            updateColorMethod.Invoke(sizeValueSignInput, new object[] { });

            // ---- Timer ----

            // Timer background picture
            timerPictureFrame = SpawnPictureFrame(
                lowerLeftPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );
            timerPictureFrame.transform.parent = rightWallRotator;

            // Timer click area
            timerClickSign = SpawnSign(
                lowerLeftClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );
            timerClickSign.transform.parent = rightWallRotator;

            // Timer label
            timerLabelSign = SpawnSign(
                lowerLeftLabelSignPos,
                rotate90,
                clickSignScale,
                "Timer",
                false
            );
            timerLabelSign.transform.parent = rightWallRotator;

            // ---- Generate ----

            // Generate background picture
            generatePictureFrame = SpawnPictureFrame(
                lowerRightPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );
            generatePictureFrame.transform.parent = rightWallRotator;

            // Generate click area
            generateClickSign = SpawnSign(
                lowerRightClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );
            generateClickSign.transform.parent = rightWallRotator;

            // Generate label
            generateLabelSign = SpawnSign(
                lowerRightLabelSignPos,
                rotate90,
                clickSignScale,
                "Generate",
                false
            );
            generateLabelSign.transform.parent = rightWallRotator;

            generateValueSign = SpawnSign(
                lowerRightLabelSignPos + new Vector3(0, -0.3f, 0),
                rotate90,
                clickSignScale * 0.75f,
                "...",
                false
            );
            generateValueSign.transform.parent = rightWallRotator;

            generateValueSignInput = generateValueSign.GetComponentInChildren<uGUI_SignInput>();
            generateValueSignInput.colors = new Color[1] { Color.white };
            generateValueSignInput.colorIndex = 0;
            generateValueSign.colorIndex = 0;

            updateColorMethod.Invoke(generateValueSignInput, new object[] { });



            rightWallRotator.rotation = Quaternion.Euler(0f, 45f, 0f);

            SubRoot currentSub = Mod.mazeBase.GetComponent<SubRoot>();
            rightWallRotator.parent = currentSub.GetModulesRoot();
            SkyEnvironmentChanged.Send(rightWallRotatorGameObject, currentSub);

            UpdateUI();
        }

        public static void SpawnFinishWall(Vector3 finishLocation)
        {
            Quaternion rotate90 = Quaternion.Euler(0f, 90f, 0f);
            Vector3 clickSignScale = new Vector3(1.85f, 1.7f, 2f);

            Vector3 giantPictureFrameOffset = new Vector3(0.6f, 0f, 5f);

            Vector3 centerPictureFramePos = new Vector3(0.615f, 0f, 5f);
            Vector3 centerClickSignPos = new Vector3(0.65f, 0f, 5f);
            Vector3 centerLabelSignPos = new Vector3(0.65f, 0f, 5f);

            // Create a new empty game object to handle rotating the other screens cleanly
            finishWallRotatorGameObject = new GameObject();
            Transform finishWallRotator = finishWallRotatorGameObject.transform;
            finishWallRotator.position = MazeHandler.basePosition + new Vector3(5f, 0f, 5f);

            // ---------- Finish wall ----------

            // Visible giant picture frame
            finishWallPictureFrame = SpawnPictureFrame(
                giantPictureFrameOffset,
                rotate90,
                Vector3.one,
                false,
                false
            );
            finishWallPictureFrame.transform.parent = finishWallRotator;

            // ---- Power ----

            // Power toggle background picture
            finishWarpPictureFrame = SpawnPictureFrame(
                centerPictureFramePos,
                rotate90,
                Vector3.one,
                true,
                false
            );
            finishWarpPictureFrame.transform.parent = finishWallRotator;

            // Power toggle click area
            finishWarpClickSign = SpawnSign(
                centerClickSignPos,
                rotate90,
                clickSignScale,
                "",
                true
            );
            finishWarpClickSign.transform.parent = finishWallRotator;

            // Power toggle label
            finishWarpLabelSign = SpawnSign(
                centerLabelSignPos,
                rotate90,
                clickSignScale,
                "Warp to Start",
                false
            );
            finishWarpLabelSign.transform.parent = finishWallRotator;

            finishWallRotator.rotation = Quaternion.Euler(0f, 90f, 0f);
            finishWallRotator.position = finishLocation + new Vector3(5f, 0, 5f);

            SubRoot currentSub = Mod.mazeBase.GetComponent<SubRoot>();
            finishWallRotator.parent = currentSub.GetModulesRoot();
            SkyEnvironmentChanged.Send(finishWallRotatorGameObject, currentSub);
        }

        public static void DestroyFinishWall()
        {
            if (finishWallRotatorGameObject != null)
            {
                Object.Destroy(finishWallRotatorGameObject);
                finishWallRotatorGameObject = null;
            }

            if (finishWallPictureFrame != null)
            {
                Object.Destroy(finishWallPictureFrame);
                finishWallPictureFrame = null;
            }

            if (finishWarpClickSign != null)
            {
                Object.Destroy(finishWarpClickSign);
                finishWarpClickSign = null;
            }

            if (finishWarpLabelSign != null)
            {
                Object.Destroy(finishWarpLabelSign);
                finishWarpLabelSign = null;
            }

            if (finishWarpPictureFrame != null)
            {
                Object.Destroy(finishWarpPictureFrame);
                finishWarpPictureFrame = null;
            }
        }

        private static Sign SpawnSign(Vector3 relativePosition, Quaternion rotation, Vector3 scale, string text, bool clickable)
        {
            GameObject gameObject = Object.Instantiate(Mod.cachedPrefabs[TechType.Sign]);

            Transform transform = gameObject.transform;
            transform.position = MazeHandler.basePosition + relativePosition;
            transform.rotation = rotation;

            Constructable constructable = gameObject.GetComponentInParent<Constructable>();
            constructable.SetState(value: true);
            constructable.SetIsInside(true);

            SubRoot currentSub = Mod.mazeBase.GetComponent<SubRoot>();
            gameObject.transform.parent = currentSub.GetModulesRoot();

            SkyEnvironmentChanged.Send(gameObject, currentSub);

            uGUI_SignInput signInput = gameObject.GetComponentInChildren<uGUI_SignInput>();
            signInput.text = text;
            signInput.SetBackground(false);

            Sign sign = gameObject.GetComponent<Sign>();
            sign.transform.localScale = scale;

            if (!clickable)
            {
                sign.boxCollider.enabled = false;
            }

            return sign;
        }

        private static PictureFrame SpawnPictureFrame(Vector3 relativePosition, Quaternion rotation, Vector3 scale, bool screenOnly, bool clickable)
        {
            GameObject gameObject = Object.Instantiate(Mod.cachedPrefabs[TechType.PictureFrame]);

            Transform transform = gameObject.transform;
            transform.position = MazeHandler.basePosition + relativePosition;
            transform.rotation = rotation;

            Constructable constructable = gameObject.GetComponentInParent<Constructable>();
            constructable.SetState(value: true);
            constructable.SetIsInside(true);

            SubRoot currentSub = Mod.mazeBase.GetComponent<SubRoot>();
            gameObject.transform.parent = currentSub.GetModulesRoot();

            SkyEnvironmentChanged.Send(gameObject, currentSub);

            PictureFrame pictureFrame = gameObject.GetComponent<PictureFrame>();
            pictureFrame.distance = 20f;
            pictureFrame.transform.localScale = scale;

            if (screenOnly)
            {
                MeshRenderer[] meshRenderers = pictureFrame.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    if (!meshRenderer.name.Equals("Screen"))
                    {
                        meshRenderer.enabled = false;
                    }
                }
            }
            
            if (!clickable)
            {
                GenericHandTarget genericHandTarget = pictureFrame.GetComponentInChildren<GenericHandTarget>();
                genericHandTarget.gameObject.SetActive(false);
            }

            return pictureFrame;
        }

        public static void UpdateUI()
        {
            if (powerPictureFrame != null)
            {
                if (PowerHandler.powerEnabled)
                {
                    powerPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Power On.png"));
                }
                else
                {
                    powerPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Power Off.png"));
                }
            }

            if (floodPictureFrame != null)
            {
                if (FloodHandler.floodingEnabled)
                {
                    floodPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Flood On.png"));
                }
                else
                {
                    floodPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Flood Off.png"));
                }
            }

            if (creaturesPictureFrame != null)
            {
                if (CreatureHandler.creaturesEnabled)
                {
                    creaturesPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Creatures On.png"));
                }
                else
                {
                    creaturesPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Creatures Off.png"));
                }
            }

            if (oxygenPictureFrame != null)
            {
                if (Mod.oxygenEnabled)
                {
                    oxygenPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Oxygen On.png"));
                }
                else
                {
                    oxygenPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Oxygen Off.png"));
                }
            }

            if (seedPictureFrame != null)
            {
                seedPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Blank.png"));
            }

            if (seedValueSignInput != null)
            {
                if (MazeHandler.seedIsRandom)
                {
                    seedValueSignInput.text = "Random";
                }
                else
                {
                    seedValueSignInput.text = MazeHandler.randomSeed.ToString();
                }
            }

            if (sizePictureFrame != null)
            {
                sizePictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Blank.png"));
            }

            if (sizeValueSignInput != null)
            {
                sizeValueSignInput.text = $"{MazeHandler.mazeSize.x},{MazeHandler.mazeSize.z}";
            }

            if (timerPictureFrame != null)
            {
                if (Mod.mazeTimerLabel.IsTimerEnabled())
                {
                    timerPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Timer On.png"));
                }
                else
                {
                    timerPictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Timer Off.png"));
                }
            }

            if (generatePictureFrame != null)
            {
                generatePictureFrame.SelectImage(System.IO.Path.Combine(Mod.ModPath, "images/Blank.png"));
            }

            if (generateValueSignInput != null)
            {
                if (MazeHandler.queuedBaseDestroys.Count > 0)
                {
                    generateValueSignInput.colors = new Color[1] { new Color(0.5f, 0.2f, 1f) };
                    updateColorMethod.Invoke(generateValueSignInput, new object[] { });
                    generateValueSignInput.text = $"Destroying base...\n{MazeHandler.queuedBaseDestroys.Count} remaining";
                }
                else if (MazeHandler.queuedBasePlacements.Count > 0)
                {
                    generateValueSignInput.text = $"Building base...\n{MazeHandler.queuedBasePlacements.Count} remaining";
                    generateValueSignInput.colors = new Color[1] { new Color(0.3f, 0.3f, 1f) };
                    updateColorMethod.Invoke(generateValueSignInput, new object[] { });
                }
                else if (MazeHandler.queuedCreaturePlacements.Count > 0)
                {
                    generateValueSignInput.text = $"Placing creatures...\n{MazeHandler.queuedCreaturePlacements.Count} remaining";
                    generateValueSignInput.colors = new Color[1] { new Color(0.8f, 0.2f, 0.2f) };
                    updateColorMethod.Invoke(generateValueSignInput, new object[] { });
                }
                else if (MazeHandler.queuedPosterPlacements.Count > 0)
                {
                    generateValueSignInput.text = $"Placing posters...\n{MazeHandler.queuedPosterPlacements.Count} remaining";
                    generateValueSignInput.colors = new Color[1] { new Color(0.9f, 0.9f, 0f) };
                    updateColorMethod.Invoke(generateValueSignInput, new object[] { });
                }
                else if (regenerationRequired)
                {
                    generateValueSignInput.text = $"Ready to\nregenerate!";
                    generateValueSignInput.colors = new Color[1] { new Color(0.8f, 0.4f, 0f) };
                    updateColorMethod.Invoke(generateValueSignInput, new object[] { });
                }
                else
                {
                    generateValueSignInput.colors = new Color[1] { new Color(0f, 0.6f, 0f) };
                    updateColorMethod.Invoke(generateValueSignInput, new object[] { });
                    generateValueSignInput.text = "Ready!";
                }
            }
        }

        public static void DestroyMazeSettingsPictureFrames()
        {
            if (middleWallPictureFrame != null)
            {
                Object.Destroy(middleWallPictureFrame);
                middleWallPictureFrame = null;
            }

            if (rightWallPictureFrame != null)
            {
                Object.Destroy(rightWallPictureFrame);
                rightWallPictureFrame = null;
            }

            if (rightWallRotatorGameObject != null)
            {
                Object.Destroy(rightWallRotatorGameObject);
                rightWallRotatorGameObject = null;
            }

            if (powerClickSign != null)
            {
                Object.Destroy(powerClickSign);
                powerClickSign = null;
            }

            if (floodClickSign != null)
            {
                Object.Destroy(floodClickSign);
                floodClickSign = null;
            }

            if (creaturesClickSign != null)
            {
                Object.Destroy(creaturesClickSign);
                creaturesClickSign = null;
            }

            if (oxygenClickSign != null)
            {
                Object.Destroy(oxygenClickSign);
                oxygenClickSign = null;
            }

            if (seedClickSign != null)
            {
                Object.Destroy(seedClickSign);
                seedClickSign = null;
            }

            if (sizeClickSign != null)
            {
                Object.Destroy(sizeClickSign);
                sizeClickSign = null;
            }

            if (timerClickSign != null)
            {
                Object.Destroy(timerClickSign);
                timerClickSign = null;
            }

            if (generateClickSign != null)
            {
                Object.Destroy(generateClickSign);
                generateClickSign = null;
            }

            if (powerLabelSign != null)
            {
                Object.Destroy(powerLabelSign);
                powerLabelSign = null;
            }

            if (floodLabelSign != null)
            {
                Object.Destroy(floodLabelSign);
                floodLabelSign = null;
            }

            if (creaturesLabelSign != null)
            {
                Object.Destroy(creaturesLabelSign);
                creaturesLabelSign = null;
            }

            if (oxygenLabelSign != null)
            {
                Object.Destroy(oxygenLabelSign);
                oxygenLabelSign = null;
            }

            if (seedLabelSign != null)
            {
                Object.Destroy(seedLabelSign);
                seedLabelSign = null;
            }

            if (sizeLabelSign != null)
            {
                Object.Destroy(sizeLabelSign);
                sizeLabelSign = null;
            }

            if (timerLabelSign != null)
            {
                Object.Destroy(timerLabelSign);
                timerLabelSign = null;
            }

            if (generateLabelSign != null)
            {
                Object.Destroy(generateLabelSign);
                generateLabelSign = null;
            }

            if (powerPictureFrame != null)
            {
                Object.Destroy(powerPictureFrame);
                powerPictureFrame = null;
            }

            if (floodPictureFrame != null)
            {
                Object.Destroy(floodPictureFrame);
                floodPictureFrame = null;
            }

            if (creaturesPictureFrame != null)
            {
                Object.Destroy(creaturesPictureFrame);
                creaturesPictureFrame = null;
            }

            if (oxygenPictureFrame != null)
            {
                Object.Destroy(oxygenPictureFrame);
                oxygenPictureFrame = null;
            }

            if (seedPictureFrame != null)
            {
                Object.Destroy(seedPictureFrame);
                seedPictureFrame = null;
            }

            if (sizePictureFrame != null)
            {
                Object.Destroy(sizePictureFrame);
                sizePictureFrame = null;
            }

            if (timerPictureFrame != null)
            {
                Object.Destroy(timerPictureFrame);
                timerPictureFrame = null;
            }

            if (generatePictureFrame != null)
            {
                Object.Destroy(generatePictureFrame);
                generatePictureFrame = null;
            }

            if (seedValueSign != null)
            {
                Object.Destroy(seedValueSign);
                seedValueSign = null;
            }

            if (seedValueSignInput != null)
            {
                Object.Destroy(seedValueSignInput);
                seedValueSignInput = null;
            }

            if (sizeValueSign != null)
            {
                Object.Destroy(sizeValueSign);
                sizeValueSign = null;
            }

            if (sizeValueSignInput != null)
            {
                Object.Destroy(sizeValueSignInput);
                sizeValueSignInput = null;
            }

            if (generateValueSign != null)
            {
                Object.Destroy(generateValueSign);
                generateValueSign = null;
            }

            if (generateValueSignInput != null)
            {
                Object.Destroy(generateValueSignInput);
                generateValueSignInput = null;
            }
        }
    }
}
