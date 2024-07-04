using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UltimatePropulsionCannon
{
    internal class ConsoleCommandListener : MonoBehaviour
    {
        public void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "upc", false, false);
        }

        public void OnConsoleCommand_upc(NotificationCenter.Notification n)
        {
            if (n == null) return;

            string command = (string)n.data[0];

            if (command == "reload")
            {
                Plugin.config = Config.Load();
                Mod.LogDebug($"Loaded {Plugin.ModName} config!");
            }
            else if (command == "save")
            {
                Plugin.config.Save();
                Mod.LogDebug($"Saved {Plugin.ModName} config!");
            }
            else if (command == "debug")
            {
                Plugin.config.debug = !Plugin.config.debug;
                Mod.LogDebug($"Toggled debug: {Plugin.config.debug}");
            }
            else if (command == "chaos")
            {
                Plugin.config.enableLandscape = !Plugin.config.enableLandscape;
                Mod.LogDebug($"Toggled enableLandscape: {Plugin.config.enableLandscape}");
            }
            else if (command == "show")
            {
                Plugin.config.showObjectName = !Plugin.config.showObjectName;
                Mod.LogDebug($"Toggled displayObjectName: {Plugin.config.showObjectName}");
            }
        }
    }
}
