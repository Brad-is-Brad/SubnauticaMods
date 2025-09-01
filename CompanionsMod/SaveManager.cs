using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CompanionsMod
{
    internal class SaveManager
    {
        static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        [HarmonyPatch(typeof(IngameMenu))]
        [HarmonyPatch("SaveGameAsync")]
        internal class Patch_IngameMenu_SaveGameAsync
        {
            [HarmonyPostfix]
            public static void Postfix(IngameMenu __instance)
            {
                Save();
            }
        }

        static void Save()
        {
            try
            {
                string slot = SaveLoadManager.main.GetCurrentSlot();
                string path = Path.Combine(Mod.ModPath, $"saves/{slot}.json");
                Plugin.Logger.LogInfo($"Saving companion to: {path}");

                Plugin.Logger.LogInfo($"curCompanion: {CompanionHandler.instance.curCompanion}");

                string contents = JsonConvert.SerializeObject(
                    CompanionHandler.instance.curCompanion,
                    Formatting.Indented,
                    serializerSettings
                );
                Plugin.Logger.LogInfo($"Saved companion: {contents}");

                File.WriteAllText(path, contents);
                Plugin.Logger.LogInfo($"Done saving companion!");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogDebug($"Error saving companion: {ex.Message}");
                Plugin.Logger.LogDebug($"\n{ex.StackTrace}");
            }
        }

        public static void Load()
        {
            try
            {
                CompanionHandler.healthBarsAdded = false;

                string slot = SaveLoadManager.main.GetCurrentSlot();
                string path = Path.Combine(Mod.ModPath, $"saves/{slot}.json");
                if (!File.Exists(path))
                {
                    Plugin.Logger.LogInfo($"No companion save file found.");
                    return;
                }

                string companionDataString = File.ReadAllText(path);
                if (companionDataString.Equals("null"))
                {
                    Plugin.Logger.LogInfo($"No companion to load.");
                    return;
                }

                JObject companionData = JsonConvert.DeserializeObject<JObject>(companionDataString, serializerSettings);

                if (companionData == null)
                {
                    throw new Exception("Could not load companion data!");
                }

                TechType companionTechType = (TechType)Int32.Parse((string)companionData["techType"]);
                GameObject gameObject = CraftData.InstantiateFromPrefab(
                    PrefabHandler.cachedPrefabs[companionTechType],
                    companionTechType
                );
                gameObject.transform.position = new Vector3(
                    float.Parse((string)companionData["lastPosition"]["x"]),
                    float.Parse((string)companionData["lastPosition"]["y"]),
                    float.Parse((string)companionData["lastPosition"]["z"])
                );

                Companion companion = gameObject.AddComponent<Companion>();

                string contents = JsonConvert.SerializeObject(
                    companion,
                    Formatting.Indented,
                    serializerSettings
                );

                Plugin.Logger.LogInfo($"SaveManager - companionData: {companionData}");
                Plugin.Logger.LogInfo($"SaveManager - Load - companionId: {(string)companionData["companionId"]}");
                CompanionHandler.instance.curCompanion = companion;

                JsonConvert.PopulateObject(File.ReadAllText(path), companion, serializerSettings);
                companion.Setup((string)companionData["companionId"]);
                CompanionHandler.instance.companionBeaconPingInstance.SetVisible(true);

                string contents2 = JsonConvert.SerializeObject(
                    companion,
                    Formatting.Indented,
                    serializerSettings
                );
                Plugin.Logger.LogInfo($"Loaded companion: {contents2}");

                companion.curLevel = companion.CalculateLevel();
                companion.UpdateStats();

                uGUI_CompanionTab.instance.UpdateCompanionName();

                Plugin.Logger.LogInfo($"Loaded companion!");

                CompanionHandler.healthBarsAdded = false;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogInfo($"Error loading companion: {ex.Message}");
                Plugin.Logger.LogInfo($"\n{ex.StackTrace}");
            }
        }

        [HarmonyPatch(typeof(IngameMenu))]
        [HarmonyPatch("QuitGameAsync")]
        internal class Patch_IngameMenu_QuitGameAsync
        {
            [HarmonyPostfix]
            public static void Postfix(IngameMenu __instance)
            {
                CompanionHandler.Quit();
                //uGUI_CompanionTab.Quit();
            }
        }
    }
}
