﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DriveAnythingMod
{
    internal class Config
    {
        public float drivingForce = 200f;
        public float turnSpeed = 5f;
        public float maxLookDistance = 1000f;

        public float rigidbodyMass = 12000f;
        public float rigidbodyAngularDrag = 1f;
        public float rigidbodyDrag = 1f;
        public List<float> rigidbodyInertiaTensor = new List<float>{ 2472782.0f, 2445512.0f, 166248.5f };
        public bool rigidbodyUseGravityAboveZero = true;
        public bool rigidbodyUseGravityBelowZero = false;

        public bool debug = false;

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
