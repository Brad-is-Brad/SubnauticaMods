using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanionsMod
{
    internal class uGUI_CompanionTab : uGUI_PDATab
    {
        public static uGUI_CompanionTab instance;

        CanvasGroup content;
        RawImage companionImage;
        string companionImageName = "";
        
        bool haveCompanion = true;
        string desiredActiveTab = "";
        string activeTab = "";
        TextMeshProUGUI rightHalfLabelText;

        GameObject stayButton;
        GameObject followButton;
        GameObject attackButton;
        GameObject releaseButton;
        GameObject renameButton;

        GameObject statsContainer;
        GameObject commandsContainer;

        TextLine companionName;
        TextLine companionType;
        ProgressBar xpBar;
        ProgressBar healthBar;
        Stat attackStat;

        GameObject statsListEntry;
        GameObject inventoryListEntry;
        bool awaitingOpenInventory;

        public void DoUpdate()
        {
            if (content == null || CompanionHandler.instance == null) return;

            if (CompanionHandler.instance.curCompanion != null && !haveCompanion)
            {
                // Enable tab elements
                if (CompanionHandler.instance.curCompanion.shouldFollow)
                {
                    stayButton.SetActive(true);
                }
                else
                {
                    followButton.SetActive(true);
                }

                attackButton.SetActive(true);
                releaseButton.SetActive(true);
                renameButton.SetActive(true);

                desiredActiveTab = "Stats";
                statsListEntry.SetActive(true);
                inventoryListEntry.SetActive(true);

                haveCompanion = true;
            }
            else if (CompanionHandler.instance.curCompanion == null && haveCompanion)
            {
                // Disable tab elements
                stayButton.SetActive(false);
                followButton.SetActive(false);
                attackButton.SetActive(false);
                releaseButton.SetActive(false);
                renameButton.SetActive(false);

                desiredActiveTab = "Commands";
                statsListEntry.SetActive(false);
                inventoryListEntry.SetActive(false);

                haveCompanion = false;
            }

            // This doesn't work in the uGUI_IListEntryManager.OnButtonDown function, so we do it here
            if (!activeTab.Equals(desiredActiveTab))
            {
                if (desiredActiveTab.Equals("Inventory"))
                {
                    Inventory.main.SetUsedStorage(CompanionHandler.instance.curCompanion.itemsContainer);
                    PDA pDA = Player.main.GetPDA();

                    pDA.Close();
                    awaitingOpenInventory = true;

                    desiredActiveTab = activeTab;
                }
                else
                {
                    statsContainer.SetActive(desiredActiveTab.Equals("Stats"));
                    commandsContainer.SetActive(desiredActiveTab.Equals("Commands"));

                    ListEntryManager.instance.Select(desiredActiveTab);

                    rightHalfLabelText.text = desiredActiveTab.ToUpper();

                    activeTab = desiredActiveTab;
                }
            }

            if (haveCompanion)
            {
                if (
                    followButton.activeSelf
                    && CompanionHandler.instance.curCompanion.shouldFollow
                    && CompanionHandler.instance.curCompanion.GetAttackTarget() == null
                )
                {
                    stayButton.SetActive(true);
                    followButton.SetActive(false);
                }
                else if (
                    stayButton.activeSelf
                    && (
                        !CompanionHandler.instance.curCompanion.shouldFollow
                        || CompanionHandler.instance.curCompanion.GetAttackTarget() != null
                    )
                )
                {
                    stayButton.SetActive(false);
                    followButton.SetActive(true);
                }

                if (xpBar != null)
                {
                    // TODO: update only when needed?
                    Companion companion = CompanionHandler.instance.curCompanion;

                    float requiredXpPrevLevel = (float)Math.Pow(companion.curLevel * 4f, 2);
                    float requiredXp = (float)Math.Pow((companion.curLevel + 1) * 4f, 2);

                    xpBar.SetProgress(
                        companion.experiencePoints - requiredXpPrevLevel,
                        requiredXp - requiredXpPrevLevel
                    );

                    xpBar.SetName($"Level {companion.curLevel}");
                }

                if (healthBar != null)
                {
                    healthBar.SetProgress(
                        CompanionHandler.instance.curCompanion.GetCreature().liveMixin.health,
                        CompanionHandler.instance.curCompanion.GetCreature().liveMixin.maxHealth
                    );
                }

                if (attackStat != null && CompanionHandler.instance.curCompanion.myMeleeAttack != null)
                {
                    attackStat.SetValue((int)Math.Floor(CompanionHandler.instance.curCompanion.myMeleeAttack.biteDamage));
                }

                if (!CompanionHandler.instance.curCompanion.GetCreature().name.Equals(companionImageName))
                {
                    SetCompanionImage(CompanionHandler.instance.curCompanion.GetCreature().name);
                }

                if (awaitingOpenInventory)
                {
                    Inventory.main.SetUsedStorage(CompanionHandler.instance.curCompanion.itemsContainer);
                    PDA pDA = Player.main.GetPDA();
                    pDA.ui.SetTabs(null);
                    if (pDA.Open(PDATab.Inventory))
                    {
                        awaitingOpenInventory = false;
                        //open = true;
                    }
                }
            }
        }

        public void UpdateCompanionName()
        {
            Plugin.Logger.LogInfo($"UpdateCompanionName - {companionName} - {companionType}");
            companionName?.SetText(CompanionHandler.instance.curCompanion.companionName);
            companionType?.SetText(CompanionHandler.instance.curCompanion.GetCleanTypeName());
            CompanionHandler.instance.companionBeaconPingInstance.SetLabel("Companion");

        }

        public void SetCompanionImage(string companionName)
        {
            if (companionImage == null) { return; }

            Plugin.Logger.LogInfo($"SetCompanionImage - {companionName} - {companionName.ToLower()}");

            string lowerCompanionName = companionName.ToLower();
            PDAEncyclopedia.EntryData entryData =
                Player.main.pdaData.encyclopedia.Find(
                    (entry) =>
                        lowerCompanionName.Contains(entry.key.ToLower())
                        || lowerCompanionName.Contains("shocker") && entry.key.ToLower().Contains("ampeel")
                        // Shuttlebug - Jumper
                )
            ;

            Plugin.Logger.LogInfo($"entryData - {entryData}");
            Plugin.Logger.LogInfo($"entryData.image - {entryData.image}");

            Plugin.Logger.LogInfo($"companionImage - {companionImage}");
            Plugin.Logger.LogInfo($"companionImage.texture - {companionImage.texture}");

            companionImage.texture = entryData.image;
            companionImageName = companionName;
        }

        public static bool Setup()
        {
            if (IngameMenu.main == null) return false;

            Plugin.Logger.LogInfo($"uGUI_CompanionTab - Setup");
            uGUI_PingTab pingTab = (uGUI_PingTab)uGUI_PDA.main.tabPing;

            // CompanionTab
            Plugin.Logger.LogInfo($"Building CompanionTab...");
            GameObject companionTabGameObject = new GameObject();
            companionTabGameObject.transform.SetParent(pingTab.transform.parent, false);
            uGUI_CompanionTab CompanionPDATab = companionTabGameObject.AddComponent<uGUI_CompanionTab>();

            // CompanionTab -> Content
            Plugin.Logger.LogInfo($"Building CompanionTab -> Content...");
            GameObject contentGameObject = new GameObject();
            contentGameObject.transform.SetParent(companionTabGameObject.transform, false);
            CompanionPDATab.content = contentGameObject.AddComponent<CanvasGroup>();

            // CompanionTab -> Content -> CompanionManagerLabel
            Plugin.Logger.LogInfo($"Building CompanionTab -> Content -> CompanionManagerLabel...");
            GameObject pingManagerLabel = pingTab.gameObject.transform.Find("Content").Find("PingManagerLabel").gameObject;

            // Right half label
            GameObject rightHalfLabel = Instantiate(pingManagerLabel);
            CompanionPDATab.rightHalfLabelText = rightHalfLabel.GetComponent<TextMeshProUGUI>();
            RectTransform rightHalfLabelRect = rightHalfLabel.GetComponent<RectTransform>();
            rightHalfLabelRect.anchoredPosition = new Vector2(200, rightHalfLabelRect.anchoredPosition.y);
            rightHalfLabel.transform.SetParent(contentGameObject.transform, false);

            // Left half tabs label
            GameObject commandsManagerLabel = Instantiate(pingManagerLabel);
            TextMeshProUGUI commandsManagerLabelText = commandsManagerLabel.GetComponent<TextMeshProUGUI>();
            commandsManagerLabelText.text = "TABS";
            RectTransform commandsManagerLabelRect = commandsManagerLabel.GetComponent<RectTransform>();
            commandsManagerLabelRect.anchoredPosition = new Vector2(-300, commandsManagerLabelRect.anchoredPosition.y);
            commandsManagerLabel.transform.SetParent(contentGameObject.transform, false);

            // Horizontal Layout
            GameObject tabElementsContainer = new GameObject();
            HorizontalLayoutGroup horizontalLayoutGroup = tabElementsContainer.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.spacing = 10f;
            horizontalLayoutGroup.childControlWidth = true;
            horizontalLayoutGroup.childForceExpandHeight = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            tabElementsContainer.transform.SetParent(contentGameObject.transform, false);
            RectTransform tabElementsRect = tabElementsContainer.GetComponent<RectTransform>();
            tabElementsRect.anchoredPosition = new Vector2(50, -20);
            tabElementsRect.sizeDelta = new Vector2(1000, 600);

            // Left Half Container
            GameObject leftHalfContainer = new GameObject();
            VerticalLayoutGroup leftHalfVerticalLayoutGroup = leftHalfContainer.AddComponent<VerticalLayoutGroup>();
            leftHalfVerticalLayoutGroup.spacing = 15f;
            leftHalfVerticalLayoutGroup.padding = new RectOffset(50, 50, 0, 0);
            leftHalfVerticalLayoutGroup.childForceExpandHeight = false;
            leftHalfVerticalLayoutGroup.childForceExpandWidth = false;
            leftHalfContainer.transform.SetParent(tabElementsContainer.transform, false);


            // Left half tabs
            ListEntryManager.instance = new ListEntryManager();

            CompanionPDATab.statsListEntry = ListEntryManager.instance.CreateListEntry(leftHalfContainer.transform, "Stats");
            GameObject commandsListEntry = ListEntryManager.instance.CreateListEntry(leftHalfContainer.transform, "Commands");
            CompanionPDATab.inventoryListEntry = ListEntryManager.instance.CreateListEntry(leftHalfContainer.transform, "Inventory");

            CompanionPDATab.desiredActiveTab = "Stats";


            CompanionPDATab.statsContainer = new GameObject();
            VerticalLayoutGroup statsVerticalLayoutGroup = CompanionPDATab.statsContainer.AddComponent<VerticalLayoutGroup>();
            statsVerticalLayoutGroup.spacing = 40f;
            statsVerticalLayoutGroup.padding = new RectOffset(100, 100, 0, 0);
            statsVerticalLayoutGroup.childForceExpandHeight = false;
            statsVerticalLayoutGroup.childForceExpandWidth = false;
            CompanionPDATab.statsContainer.transform.SetParent(tabElementsContainer.transform, false);


            GameObject imageContainer = new GameObject();
            CompanionPDATab.companionImage = imageContainer.AddComponent<RawImage>();
            LayoutElement companionImageLayoutElement = imageContainer.AddComponent<LayoutElement>();
            companionImageLayoutElement.preferredWidth = 500f;
            companionImageLayoutElement.preferredHeight = 250f;
            imageContainer.transform.SetParent(CompanionPDATab.statsContainer.transform, false);

            CompanionPDATab.companionName = new TextLine(CompanionPDATab.statsContainer.transform, "", null, 32);
            CompanionPDATab.companionType = new TextLine(CompanionPDATab.statsContainer.transform, "", Color.white, 24);
            CompanionPDATab.xpBar = new ProgressBar(CompanionPDATab.statsContainer.transform, "Level 1", new Color(.9f, .8f, .3f), 500f);
            CompanionPDATab.healthBar = new ProgressBar(CompanionPDATab.statsContainer.transform, "Health", new Color(.64f, .85f, .41f), 500f);
            CompanionPDATab.attackStat = new Stat(CompanionPDATab.statsContainer.transform, "Attack");


            // Command Buttons Vertical Layout
            CompanionPDATab.commandsContainer = new GameObject();

            VerticalLayoutGroup commandButtonsVerticalLayoutGroup = CompanionPDATab.commandsContainer.AddComponent<VerticalLayoutGroup>();
            commandButtonsVerticalLayoutGroup.spacing = 15f;
            commandButtonsVerticalLayoutGroup.childForceExpandHeight = false;
            commandButtonsVerticalLayoutGroup.childForceExpandWidth = false;
            commandButtonsVerticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;

            // TODO: this width is a hack to get this to center properly
            LayoutElement commandButtonsLayoutElement = CompanionPDATab.commandsContainer.AddComponent<LayoutElement>();
            commandButtonsLayoutElement.preferredWidth = 700f;

            CompanionPDATab.commandsContainer.transform.SetParent(tabElementsContainer.transform, false);

            CreateButton(CompanionPDATab.commandsContainer, "Capture", (data) => {
                Player.main.GetPDA().Close();
                CompanionHandler.instance.awaitingAttackTarget = false;
                CompanionHandler.instance.awaitingCaptureTarget = true;
            });

            CompanionPDATab.stayButton = CreateButton(CompanionPDATab.commandsContainer, "Stay", (data) => {
                CompanionHandler.instance.curCompanion.Stay();
            });

            CompanionPDATab.followButton = CreateButton(CompanionPDATab.commandsContainer, "Follow", (data) => {
                CompanionHandler.instance.curCompanion.Follow();
            });

            CompanionPDATab.attackButton = CreateButton(CompanionPDATab.commandsContainer, "Attack", (data) => {
                CompanionHandler.instance.awaitingCaptureTarget = false;
                CompanionHandler.instance.awaitingAttackTarget = true;
                Player.main.GetPDA().Close();
            });

            CompanionPDATab.releaseButton = CreateButton(CompanionPDATab.commandsContainer, "Release", (data) => {
                CompanionHandler.instance.Release();
            });

            CompanionPDATab.renameButton = CreateButton(CompanionPDATab.commandsContainer, "Rename", (data) => {
                uGUI.main.userInput.RequestString(
                        "Companion Name",
                        "Set",
                        CompanionHandler.instance.curCompanion.companionName,
                        100,
                        (string newName) =>
                        {
                            CompanionHandler.instance.curCompanion.companionName = newName;
                            CompanionPDATab.UpdateCompanionName();
                        }
                    );
            });


            Plugin.Logger.LogInfo($"Building done!");


            // Make sure the tab always appears along with the other regular PDA tabs
            PDATab CompanionPDATabEnum = (PDATab)8;
            FieldInfo regularTabsField = typeof(uGUI_PDA)
                .GetField("regularTabs", BindingFlags.NonPublic | BindingFlags.Static);
            List<PDATab> regularTabs = new List<PDATab> {
                PDATab.Inventory,
                PDATab.Journal,
                PDATab.Ping,
                PDATab.Gallery,
                PDATab.Log,
                PDATab.Encyclopedia,
                CompanionPDATabEnum
            };
            regularTabsField.SetValue(null, regularTabs);

            // Make the PDA tab clickable
            FieldInfo tabsProperty = typeof(uGUI_PDA)
                .GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<PDATab, uGUI_PDATab> tabs = (Dictionary<PDATab, uGUI_PDATab>)tabsProperty.GetValue(uGUI_PDA.main);
            tabs.Add(CompanionPDATabEnum, CompanionPDATab);
            tabsProperty.SetValue(uGUI_PDA.main, tabs);

            uGUI_PDA.main.SetTabs(regularTabs);

            CompanionPDATab.content.SetVisible(visible: false);

            instance = CompanionPDATab;

            return true;
        }

        static GameObject CreateButton(GameObject parent, string buttonText, UnityAction<BaseEventData> callback)
        {
            GameObject button = Instantiate(IngameMenu.main.transform.Find("Main").Find("ButtonLayout").Find("ButtonBack").gameObject);
            button.transform.SetParent(parent.transform, false);

            TextMeshProUGUI text = button.transform.Find("Text").gameObject.GetComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 24;

            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 200f;
            layoutElement.preferredHeight = 50f;

            EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerUp;
            entry.callback.AddListener(callback);
            eventTrigger.triggers.Clear();
            eventTrigger.triggers.Add(entry);

            return button;
        }

        public void Quit()
        {
            instance = null;
        }

        class TextLine
        {
            TextMeshProUGUI textMesh;

            public TextLine(Transform parent, string text, Color? color, int? size)
            {
                uGUI_PingTab pingTab = (uGUI_PingTab)uGUI_PDA.main.tabPing;
                GameObject pingManagerLabel = pingTab.gameObject.transform.Find("Content").Find("PingManagerLabel").gameObject;

                // Horizontal Layout
                GameObject container = new GameObject();
                LayoutElement containerLayoutElement = container.AddComponent<LayoutElement>();
                containerLayoutElement.preferredWidth = 500;
                containerLayoutElement.preferredHeight = 10;
                container.transform.SetParent(parent, false);
                HorizontalLayoutGroup containerHorizontalLayoutGroup = container.AddComponent<HorizontalLayoutGroup>();
                containerHorizontalLayoutGroup.childForceExpandHeight = false;

                // Stat Name Label
                GameObject label = Instantiate(pingManagerLabel);
                textMesh = label.GetComponent<TextMeshProUGUI>();
                textMesh.text = text;
                textMesh.alignment = TextAlignmentOptions.Center;
                if (size != null) { textMesh.fontSize = (float)size; }
                if (color != null) { textMesh.color = (Color)color; }
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchoredPosition = new Vector2(0, 0);

                label.transform.SetParent(container.transform, false);
                Destroy(label.transform.Find("Underline").gameObject);
            }

            public void SetText(string newValue)
            {
                textMesh.text = $"{newValue}";
            }
        }

        class Stat
        {
            TextMeshProUGUI valueText;

            public Stat(Transform parent, string name)
            {
                uGUI_PingTab pingTab = (uGUI_PingTab)uGUI_PDA.main.tabPing;
                GameObject pingManagerLabel = pingTab.gameObject.transform.Find("Content").Find("PingManagerLabel").gameObject;

                // Horizontal Layout
                GameObject statContainer = new GameObject();
                LayoutElement statContainerLayoutElement = statContainer.AddComponent<LayoutElement>();
                statContainerLayoutElement.preferredWidth = 500;
                statContainer.transform.SetParent(parent, false);
                HorizontalLayoutGroup statContainerHorizontalLayoutGroup = statContainer.AddComponent<HorizontalLayoutGroup>();
                statContainerHorizontalLayoutGroup.childForceExpandHeight = false;
                statContainerHorizontalLayoutGroup.childForceExpandWidth = false;

                // Stat Name Label
                GameObject label = Instantiate(pingManagerLabel);
                TextMeshProUGUI nameText = label.GetComponent<TextMeshProUGUI>();
                nameText.text = name;
                nameText.alignment = TextAlignmentOptions.Left;
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchoredPosition = new Vector2(0, 0);
                LayoutElement labelLayoutElement = label.AddComponent<LayoutElement>();
                labelLayoutElement.preferredWidth = 100f;

                label.transform.SetParent(statContainer.transform, false);
                Destroy(label.transform.Find("Underline").gameObject);

                // Progress Prefab
                uGUI_BlueprintsTab blueprintsTab = (uGUI_BlueprintsTab)uGUI_PDA.main.tabJournal;
                MethodInfo getEntryMethod = typeof(uGUI_BlueprintsTab).GetMethod("GetEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                uGUI_BlueprintEntry blueprintEntry = (uGUI_BlueprintEntry)getEntryMethod.Invoke(blueprintsTab, new object[] { TechType.Titanium });
                GameObject prefabAmount = Instantiate(blueprintEntry.prefabProgress.transform.Find("Amount").gameObject);
                
                LayoutElement prefabAmountLayoutElement = prefabAmount.AddComponent<LayoutElement>();
                prefabAmountLayoutElement.preferredWidth = 100f;

                valueText = prefabAmount.GetComponent<TextMeshProUGUI>();
                valueText.alignment = TextAlignmentOptions.Right;
                valueText.text = "0";

                RectTransform amountRect = prefabAmount.GetComponent<RectTransform>();
                amountRect.position = new Vector3(0, 0, 0);
                amountRect.sizeDelta = new Vector2(500, amountRect.sizeDelta.y);

                prefabAmount.transform.SetParent(statContainer.transform, false);
            }

            public void SetValue(int amount)
            {
                valueText.text = $"{amount}";
            }
        }

        public class ProgressBar
        {
            TextMeshProUGUI nameText;
            public TMP_Text progressText;
            RectTransform barFillRect;
            float width;

            public ProgressBar(Transform parent, string name, Color color, float width)
            {
                this.width = width;

                uGUI_PingTab pingTab = (uGUI_PingTab)uGUI_PDA.main.tabPing;
                GameObject pingManagerLabel = pingTab.gameObject.transform.Find("Content").Find("PingManagerLabel").gameObject;

                // Vertical Layout
                GameObject barContainer = new GameObject();
                LayoutElement barContainerLayoutElement = barContainer.AddComponent<LayoutElement>();
                barContainerLayoutElement.preferredWidth = width;
                barContainer.transform.SetParent(parent, false);
                VerticalLayoutGroup barContainerVerticalLayoutGroup = barContainer.AddComponent<VerticalLayoutGroup>();
                barContainerVerticalLayoutGroup.childControlHeight = true;
                barContainerVerticalLayoutGroup.childForceExpandHeight = false;
                barContainerVerticalLayoutGroup.childForceExpandWidth = false;

                // Bar Name Label
                GameObject label = Instantiate(pingManagerLabel);
                nameText = label.GetComponent<TextMeshProUGUI>();
                nameText.text = name;
                nameText.alignment = TextAlignmentOptions.Left;

                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.position = new Vector3(0, 0, 0);
                labelRect.sizeDelta = new Vector2(width, labelRect.sizeDelta.y);
                label.transform.SetParent(barContainer.transform, false);
                Destroy(label.transform.Find("Underline").gameObject);

                // Progress Prefab
                uGUI_BlueprintsTab blueprintsTab = (uGUI_BlueprintsTab)uGUI_PDA.main.tabJournal;
                MethodInfo getEntryMethod = typeof(uGUI_BlueprintsTab).GetMethod("GetEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                uGUI_BlueprintEntry blueprintEntry = (uGUI_BlueprintEntry)getEntryMethod.Invoke(blueprintsTab, new object[] { TechType.Titanium });
                GameObject prefabProgress = Instantiate(blueprintEntry.prefabProgress);

                RectTransform prefabProgressRect = prefabProgress.GetComponent<RectTransform>();
                prefabProgressRect.position = new Vector2(0, -70);

                prefabProgress.transform.Find("Frame").GetComponent<RectTransform>().sizeDelta = new Vector2(width, 20);

                TextMeshProUGUI amountText = prefabProgress.transform.Find("Amount").GetComponent<TextMeshProUGUI>();
                amountText.alignment = TextAlignmentOptions.Right;

                RectTransform amountRect = prefabProgress.transform.Find("Amount").GetComponent<RectTransform>();
                amountRect.position = new Vector3(0, 0, 0);
                amountRect.sizeDelta = new Vector2(width, amountRect.sizeDelta.y);

                uGUI_BlueprintProgress blueprintProgress = prefabProgress.GetComponent<uGUI_BlueprintProgress>();
                blueprintProgress.SetValue(0, 1);
                progressText = blueprintProgress.amount;
                progressText.text = "0 / 0";

                GameObject barBackground = new GameObject();
                Image barBackgroundImage = barBackground.AddComponent<Image>();
                barBackgroundImage.color = new Color(.1f, .1f, .1f);
                RectTransform barBackgroundRect = barBackground.GetComponent<RectTransform>();
                barBackgroundRect.position = new Vector2(2, 55);
                barBackgroundRect.sizeDelta = new Vector2(width - 20, 10);
                barBackground.transform.SetParent(prefabProgress.transform, false);

                GameObject barFill = new GameObject();
                Image barFillImage = barFill.AddComponent<Image>();
                barFillImage.color = color;

                barFillRect = barFill.GetComponent<RectTransform>();
                if (barFillRect == null)
                    barFillRect = barFill.AddComponent<RectTransform>();

                barFillRect.position = new Vector2(2, 55);
                barFillRect.sizeDelta = new Vector2(width - 20, 10);
                barFill.transform.SetParent(prefabProgress.transform, false);

                prefabProgress.transform.SetParent(barContainer.transform, false);
            }

            public void SetName(string name)
            {
                nameText.text = name;
            }

            public void SetProgress(float amount, float max)
            {
                progressText.text = $"{(int)amount} / {(int)max}";
                float progress = amount / max;

                barFillRect.anchoredPosition = new Vector3(2 - (((width - 20) - ((width - 20) * progress)) / 2), 55);
                barFillRect.sizeDelta = new Vector2((width - 20) * progress, 10);
            }
        }

        public override void Open()
        {
            content.SetVisible(visible: true);
        }

        public override void Close()
        {
            content.SetVisible(visible: false);
        }

        class ListEntryManager : uGUI_IListEntryManager
        {
            Dictionary<string, uGUI_ListEntry> infoListEntries = new Dictionary<string, uGUI_ListEntry>();
            public static ListEntryManager instance;

            public ListEntryManager()
            {
                Plugin.Logger.LogInfo($"Creating ListEntryManager!");
            }

            public GameObject CreateListEntry(Transform parent, string name)
            {
                uGUI_EncyclopediaTab encyclopediaTab = (uGUI_EncyclopediaTab)uGUI_PDA.main.tabEncyclopedia;

                GameObject entryGameObject = Instantiate(encyclopediaTab.prefabEntry, parent);

                uGUI_ListEntry infoListEntry = entryGameObject.GetComponent<uGUI_ListEntry>();
                infoListEntry.Initialize(instance, name, encyclopediaTab.entryNodeSprites);
                infoListEntry.SetIcon(encyclopediaTab.iconExpand);
                infoListEntry.SetIndent(0);
                infoListEntry.SetText(name);

                LayoutElement layoutElement = entryGameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 200f;

                infoListEntries.Add(name, infoListEntry);

                return entryGameObject;
            }

            public bool OnButtonDown(string key, GameInput.Button button)
            {
                uGUI_CompanionTab.instance.desiredActiveTab = key;
                return true;
            }

            public void Select(string key)
            {
                infoListEntries.ToList().ForEach(entry => { entry.Value.SetSelected(entry.Key.Equals(key)); });
            }
        }

        [HarmonyPatch(typeof(SpriteManager))]
        [HarmonyPatch("Get")]
        [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(Sprite) })]
        internal class Patch_SpriteManager_Get
        {
            [HarmonyPrefix]
            public static void Prefix(string atlasName, ref string name, Sprite defaultSprite)
            {
                if (name.Equals("Tab8"))
                {
                    name = "TabInventory";
                }
            }
        }

        [HarmonyPatch(typeof(TooltipFactory))]
        [HarmonyPatch("Label")]
        internal class Patch_TooltipFactory_Label
        {
            [HarmonyPrefix]
            public static bool Prefix(string label, StringBuilder sb)
            {
                if (label.Equals("Tab8"))
                {
                    sb.AppendFormat("<size=25><color=#ffffffff>{0}</color></size>", "Companion");
                    return false;
                }

                return true;
            }
        }
    }
}
