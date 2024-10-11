using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace SpookySubnautica
{
    [BepInPlugin("com.bradisbrad.SubnauticaMods.SpookySubnautica", "SpookySubnautica", "1.0.0")]
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
                ModName = ($"{assembly.GetName().Name}");
                Logger.LogInfo($"{ModName} loaded!");
                Harmony harmony = new Harmony(ModName);
                harmony.PatchAll(assembly);
                Logger.LogInfo($"{ModName} patched!");

                config = SpookySubnautica.Config.Load();
            }
            catch (System.Exception e)
            {
                Logger.LogInfo($"{e}");
            }
        }
    }
}
