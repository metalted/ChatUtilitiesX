using HarmonyLib;
using UnityEngine;

namespace ChatUtilities.Patches
{
    [HarmonyPatch(typeof(OnlineChatUI), "Update")]
    public class OnlineChatUIUpdatePatch
    {
        public static bool Prefix()
        {
            // Clean up expired suppression flag
            if (!Plugin.ShouldSuppressEnterKey())
            {
                Plugin.ClearEnterKeyConsumption();
                return true; // Allow normal execution
            }

            // If ChatUtilities consumed Enter and it's being pressed this frame, skip game's Update
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                return false; // Skip game's Update to prevent double-trigger
            }

            return true; // Allow normal execution
        }
    }
}