using HarmonyLib;
using System.Reflection;

namespace MazeGeneratorMod
{
    internal class PowerHandler
    {
        private static bool lastPowerEnabled = true;
        public static bool powerEnabled = true;

        [HarmonyPatch(typeof(PowerRelay))]
        [HarmonyPatch("UpdatePowerState")]
        internal class Patch_PowerRelay_UpdatePowerState
        {
            [HarmonyPrefix]
            public static bool Prefix(PowerRelay __instance)
            {
                Base powerRelayBase = __instance.GetComponent<Base>();
                if (powerRelayBase != null && powerRelayBase == Mod.mazeBase)
                {
                    UpdatePowerRelay(__instance);
                    return false;
                }

                return true;
            }
        }

        public static void ToggleBasePower()
        {
            if (Mod.mazeBase == null) { return; }

            powerEnabled = !powerEnabled;
        }

        public static void UpdatePowerRelay(PowerRelay powerRelay)
        {
            FieldInfo isPoweredField = typeof(PowerRelay).GetField("isPowered", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo powerStatusField = typeof(PowerRelay).GetField("powerStatus", BindingFlags.NonPublic | BindingFlags.Instance);

            if (powerEnabled)
            {
                isPoweredField.SetValue(powerRelay, true);
                powerStatusField.SetValue(powerRelay, PowerSystem.Status.Normal);
            }
            else
            {
                isPoweredField.SetValue(powerRelay, false);
                powerStatusField.SetValue(powerRelay, PowerSystem.Status.Offline);
            }

            if (lastPowerEnabled != powerEnabled)
            {
                if (powerEnabled)
                {
                    powerRelay.powerUpEvent.Trigger(powerRelay);
                }
                else
                {
                    powerRelay.powerDownEvent.Trigger(powerRelay);
                }

                lastPowerEnabled = powerEnabled;
            }
        }
    }
}
