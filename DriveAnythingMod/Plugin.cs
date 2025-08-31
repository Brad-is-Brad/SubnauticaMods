using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace DriveAnythingMod
{
    [BepInPlugin("com.bradisbrad.SubnauticaMods.DriveAnythingMod", "DriveAnythingMod", "0.0.6")]
    public class Plugin : BaseUnityPlugin
    {
        public static string ModName;
        internal static new ManualLogSource Logger;
        internal static Config config;

        private void Awake()
        {
            Logger = base.Logger;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var modName = ($"{assembly.GetName().Name}");
                Logger.LogInfo($"{modName} loaded!");
                Harmony harmony = new Harmony(modName);
                harmony.PatchAll(assembly);
                Logger.LogInfo($"{modName} patched!");

                config = DriveAnythingMod.Config.Load();
            }
            catch (System.Exception e)
            {
                Logger.LogInfo($"{e}");
            }
        }
    }
}
