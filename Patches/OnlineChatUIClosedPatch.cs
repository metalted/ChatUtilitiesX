using HarmonyLib;

namespace ChatUtilities.Patches
{
    [HarmonyPatch(typeof(OnlineChatUI), "EnableSmallBox")]
    public class OnlineChatUIClosedPatch
    {
        public static void Postfix()
        {
            if (Plugin.Instance != null)
            {
                Plugin.Instance.ChatWasClosed();
            }
        }
    }
}
