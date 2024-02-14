using System;
using UnityEngine;

namespace SeaLevelMod
{
    class ConsoleCommandListener : MonoBehaviour
    {
        public void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "sealevel", false, false);
            DevConsole.RegisterConsoleCommand(this, "sealevelspeed", false, false);
            DevConsole.RegisterConsoleCommand(this, "sealevelchange", false, false);
            DevConsole.RegisterConsoleCommand(this, "sealeveltide", false, false);
        }

        public void OnConsoleCommand_sealevel(NotificationCenter.Notification n)
        {
            Plugin.config.seaLevel = float.Parse((string)n.data[0]);
            Plugin.config.hasTide = false;
            Mod.UpdateSeaLevel();
        }

        public void OnConsoleCommand_sealevelspeed(NotificationCenter.Notification n)
        {
            Plugin.config.seaLevelChangeSpeed = float.Parse((string)n.data[0]);
            Plugin.config.hasTide = false;
        }

        public void OnConsoleCommand_sealevelchange(NotificationCenter.Notification n)
        {
            Plugin.config.seaLevel += float.Parse((string)n.data[0]);
            Mod.UpdateSeaLevel();
        }

        public void OnConsoleCommand_sealeveltide(NotificationCenter.Notification n)
        {
            //SeaLevelMod.seaLevel += float.Parse((string)n.data[0]);
            float highTide = float.Parse((string)n.data[0]);
            float lowTide = float.Parse((string)n.data[1]);
            float tidePeriod = float.Parse((string)n.data[2]);

            Plugin.config.highTide = Math.Max(highTide, lowTide);
            Plugin.config.lowTide = Math.Min(highTide, lowTide);
            Plugin.config.tidePeriod = tidePeriod;
            Plugin.config.hasTide = true;
        }
    }
}
