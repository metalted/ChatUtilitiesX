using HarmonyLib;
using UnityEngine;

namespace ChatUtilities.Patches
{
    [HarmonyPatch(typeof(OnlineChatUI), "Awake")]
    public class OnlineChatUIBindPatch
    {
        public static void Postfix(OnlineChatUI __instance)
        {
            if (Plugin.Instance == null)
            {
                return;
            }

            if ((UnityEngine.Object)__instance == null)
            {
                return;
            }

            Plugin.Instance.SetOnlineChatUI(__instance);
        }
    }
}
