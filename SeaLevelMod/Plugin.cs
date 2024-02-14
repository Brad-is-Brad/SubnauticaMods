using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace SeaLevelMod
{
    [BepInPlugin("com.bradisbrad.SeaLevelMod", "SeaLevelMod", "1.3.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Config config;

        private void Awake()
        {
            Logger = base.Logger;
            var assembly = Assembly.GetExecutingAssembly();
            var modName = ($"{assembly.GetName().Name}");
            Logger.LogInfo($"{modName} loaded!");
            Harmony harmony = new(modName);
            harmony.PatchAll(assembly);
            Logger.LogInfo($"{modName} patched!");

            config = SeaLevelMod.Config.Load();
        }
    }
}