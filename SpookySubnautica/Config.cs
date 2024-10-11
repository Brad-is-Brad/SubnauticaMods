using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SpookySubnautica
{
    internal class Config
    {
        public Dictionary<string, Dictionary<string, List<float>>> configColors = new Dictionary<string, Dictionary<string, List<float>>>();

        public static Config Load()
        {
            try
            {
                string path = Path.Combine(Mod.ModPath, "config.json");
                if (!File.Exists(path))
                {
                    Plugin.Logger.LogInfo($"No config file found. Creating...");
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
                    Plugin.Logger.LogInfo($"Loaded config!");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogInfo($"Error loading config: {ex.Message}");
                Plugin.Logger.LogInfo($"\n{ex.StackTrace}");
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
                Plugin.Logger.LogInfo($"Saved config!");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogInfo($"Error saving config: {ex.Message}");
                Plugin.Logger.LogInfo($"\n{ex.StackTrace}");
            }
        }
    }
}
