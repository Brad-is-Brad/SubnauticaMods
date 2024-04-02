using Newtonsoft.Json;
using System;
using System.IO;

namespace RainbowVehicles
{
    internal class Config
    {
        // Seamoth
        public float seamothChangeSpeed = 100f;
        public bool changeSeamothMain = true;
        public bool changeSeamothName = true;
        public bool changeSeamothInterior = true;
        public bool changeSeamothStripe1 = true;
        public bool changeSeamothStripe2 = true;

        // Cyclops
        public float cyclopsChangeSpeed = 100f;
        public bool changeCyclopsBase = true;
        public bool changeCyclopsStripe1 = true;
        public bool changeCyclopsStripe2 = true;
        public bool changeCyclopsName = true;

        // Prawn Suit
        public float prawnSuitChangeSpeed = 100f;
        public bool changePrawnSuitBase = true;
        public bool changePrawnSuitName = true;
        public bool changePrawnSuitInterior = true;
        public bool changePrawnSuitStripe1 = true;
        public bool changePrawnSuitStripe2 = true;

        // Rocket
        public float rocketChangeSpeed = 100f;
        public bool changeRocketBase = true;
        public bool changeRocketStripe1 = true;
        public bool changeRocketStripe2 = true;
        public bool changeRocketName = true;

        public static Config Load()
        {
            try
            {
                string path = Path.Combine(Mod.ModPath, "config.json");
                if (!File.Exists(path))
                {
                    Plugin.Logger.LogDebug($"No config file found. Creating...");
                    Config config = new Config();
                    config.Save();
                    return config;
                }
                else
                {
                    Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
                    if (config == null)
                    {
                        throw new Exception("Could not load config.");
                    }
                    return config;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogDebug($"Error loading config: {ex.Message}");
                Plugin.Logger.LogDebug($"\n{ex.StackTrace}");
                return null;
            }
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(Mod.ModPath, "config.json");
                string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, contents);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogDebug($"Error saving config: {ex.Message}");
                Plugin.Logger.LogDebug($"\n{ex.StackTrace}");
            }
        }
    }
}
