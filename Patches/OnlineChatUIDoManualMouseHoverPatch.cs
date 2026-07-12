using HarmonyLib;
using UnityEngine;

namespace ChatUtilities.Patches
{
    [HarmonyPatch(typeof(OnlineChatUI), "DoManualMouseHover")]
    public class OnlineChatUIDoManualMouseHoverPatch
    {
        public static bool Prefix()
        {
            // If suggestion panel is open, skip all link detection
            if (Plugin.Instance != null &&
                Plugin.Instance.GetSuggestionController() != null &&
                Plugin.Instance.GetSuggestionController().IsOpen)
            {
                // Reset cursor hover state
                if (PlayerManager.Instance != null && PlayerManager.Instance.cursorManager != null)
                {
                    PlayerManager.Instance.cursorManager.SetHover(false);
                }

                return false; // Skip original method completely
            }

            return true; // Allow normal execution
        }
    }
}