using HarmonyLib;

namespace ChatUtilities.Patches
{
    [HarmonyPatch(typeof(OnlineChatUI), "SendChatMessage")]
    public class OnlineChatUISendMessagePatch
    {
        public static void Prefix(ref string message)
        {
            if (Plugin.Instance != null)
            {
                message = Plugin.Instance.ExpandShortcodesForSend(message);
            }
        }

        public static void Postfix(ref string message)
        {
            if (Plugin.Instance != null)
            {
                Plugin.Instance.MessageWasSent(message);
            }
        }
    }
}
