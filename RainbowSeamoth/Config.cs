using Newtonsoft.Json;
using System;
using System.IO;

namespace RainbowSeamoth
{
    internal class Config
    {
        public float changeSpeed = 100f;
        public bool changeMain = true;
        public bool changeName = true;
        public bool changeInterior = true;
        public bool changeStripe1 = true;
        public bool changeStripe2 = true;

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
