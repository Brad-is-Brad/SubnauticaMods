using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpookySubnautica
{
    class ConsoleCommandListener : MonoBehaviour
    {
        public void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "spooky", false, false);
        }

        public void OnConsoleCommand_spooky(NotificationCenter.Notification n)
        {
            if (n == null) { return; }

            string command = (string)n.data[0];

            if (command.Equals("reload"))
            {
                Plugin.config = Config.Load();
                Plugin.Logger.LogInfo($"Reloaded {Plugin.ModName} config!");
            }
        }
    }
}
