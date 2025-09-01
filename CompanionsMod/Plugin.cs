using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace CompanionsMod
{
    [BepInPlugin("com.bradisbrad.SubnauticaMods.CompanionsMod", "CompanionsMod", "0.0.5")]
    public class Plugin : BaseUnityPlugin
    {
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

                config = CompanionsMod.Config.Load();
            }
            catch (System.Exception e)
            {
                Logger.LogInfo($"{e}");
            }
        }
    }
}