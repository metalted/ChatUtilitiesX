using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUtilities.Patches
{
    [HarmonyPatch(typeof(MainMenuUI), "Awake")]
    internal class MainMenuAwakePatch
    {
        public static void Prefix()
        {
            Plugin.Instance.RefreshRegisteredCommands();
        }
    }
}
