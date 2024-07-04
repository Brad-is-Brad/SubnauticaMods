using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;

namespace UltimatePropulsionCannon
{
    [BepInPlugin("com.bradisbrad.SubnauticaMods.UltimatePropulsionCannon", "UltimatePropulsionCannon", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static string ModPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ModName;

        internal static Config config;
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                ModName = ($"{assembly.GetName().Name}");
                Logger.LogInfo($"{ModName} loaded!");
                Harmony harmony = new Harmony(ModName);
                harmony.PatchAll(assembly);
                Logger.LogInfo($"{ModName} patched!");

                config = UltimatePropulsionCannon.Config.Load();
                Logger.LogInfo($"{ModName} config loaded!");
            }
            catch (Exception e)
            {
                Logger.LogInfo($"{e}");
            }
        }
    }
}
