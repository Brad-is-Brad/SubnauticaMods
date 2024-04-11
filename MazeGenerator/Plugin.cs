using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace MazeGeneratorMod
{
    [BepInPlugin("com.bradisbrad.Subnautica.MazeGenerator", "MazeGenerator", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

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
            }
            catch (System.Exception e)
            {
                Logger.LogInfo($"{e}");
            }
        }
    }
}