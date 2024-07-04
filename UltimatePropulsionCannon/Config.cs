using Newtonsoft.Json;
using System;
using System.IO;

namespace UltimatePropulsionCannon
{
    internal class Config
    {
        public bool debug = false;
        public bool enableLandscape = false;
        public bool showObjectName = false;

        public float pickupDistance = 1000f;
        public float maxMass = 1_000_000f; // Game default: 1200f;
        public float maxAABBVolume = 1_000_000f; // Game default: 120f;

        public static Config Load()
        {
            try
            {
                string path = Path.Combine(Plugin.ModPath, "config.json");
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
                Plugin.Logger.LogError($"Error loading config: {ex.Message}");
                Plugin.Logger.LogError($"\n{ex.StackTrace}");
                return null;
            }
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(Plugin.ModPath, "config.json");
                string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, contents);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving config: {ex.Message}");
                Plugin.Logger.LogError($"\n{ex.StackTrace}");
            }
        }
    }
}
