using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace RainbowVehicles
{
    [BepInPlugin("com.bradisbrad.RainbowVehicles", "RainbowVehicles", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Config config;

        private void Awake()
        {
            try
            {
                Logger = base.Logger;
                var assembly = Assembly.GetExecutingAssembly();
                var modName = ($"{assembly.GetName().Name}");
                Logger.LogInfo($"{modName} loaded!");
                Harmony harmony = new Harmony(modName);
                harmony.PatchAll(assembly);
                Logger.LogInfo($"{modName} patched!");

                config = RainbowVehicles.Config.Load();
            }
            catch (System.Exception e)
            {
                Logger.LogInfo($"{e}");
            }
        }
    }
}