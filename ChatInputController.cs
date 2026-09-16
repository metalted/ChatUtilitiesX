using TMPro;
using UnityEngine;
using ZeepSDK.Chat;

namespace ChatUtilities
{
    public class ChatInputController
    {
        public OnlineChatUI OnlineChatUi { get; private set; }
        public GameObject ChatRoot { get; private set; }
        public GameObject ChatTextTemplate { get; private set; }
        public TextMeshProUGUI ChatText { get; private set; }

        public bool IsAvailable
        {
            get
            {
                return (UnityEngine.Object)OnlineChatUi != null &&
                       (UnityEngine.Object)ChatRoot != null &&
                       (UnityEngine.Object)ChatTextTemplate != null &&
                       (UnityEngine.Object)ChatText != null;
            }
        }

        public bool IsOpen
        {
            get
            {
                return IsAvailable && ChatRoot.activeInHierarchy;
            }
        }

        public void OpenChat()
        {
            if(!IsAvailable)
            {
                return;
            }

            if(IsOpen)
            {
                return;
            }

            OnlineChatUi.currentlyTyping = true;

            OnlineChatUI.wasTyping = true;
            OnlineChatUI.currentMessage = string.Empty;

            OnlineChatUi.bigChatBox.text = string.Empty;
            OnlineChatUi.caretTicker = 0f;
            OnlineChatUi.caretYes = true;

            OnlineChatUi.EnableBigBox();
        }

        public void SetText(string value)
        {
            if (!IsAvailable)
            {
                return;
            }

            if(!IsOpen)
            {
                OpenChat();
            }

            string safeValue = value ?? string.Empty;
            OnlineChatUI.currentMessage = safeValue;
            ChatText.text = safeValue;
        }

        public void Send()
        {
            if (!IsAvailable)
            {
                return;
            }

            string message = OnlineChatUI.currentMessage;

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            OnlineChatUi.SendChatMessage(message);

            OnlineChatUI.currentMessage = string.Empty;
            OnlineChatUI.wasTyping = false;

            OnlineChatUi.EnableSmallBox(true);
        }

        public void Bind(OnlineChatUI onlineChatUi)
        {
            Unbind();

            if ((UnityEngine.Object)onlineChatUi == null)
            {
                return;
            }

            OnlineChatUi = onlineChatUi;
            ChatRoot = FindDirectChild(onlineChatUi.gameObject, "Big Chat Box Input");

            if ((UnityEngine.Object)ChatRoot == null)
            {
                Debug.LogError("Chat Utilities: Big Chat Box Input was not found.");
                Unbind();
                return;
            }

            Transform chatHere = ChatRoot.transform.Find("Chat Here");

            if (chatHere == null)
            {
                Debug.LogError("Chat Utilities: Chat Here was not found.");
                Unbind();
                return;
            }

            ChatTextTemplate = chatHere.gameObject;
            ChatText = ChatTextTemplate.GetComponent<TextMeshProUGUI>();

            if ((UnityEngine.Object)ChatText == null)
            {
                Debug.LogError("Chat Utilities: Chat Here does not have a TextMeshProUGUI component.");
                Unbind();
                return;
            }
        }

        public void Unbind()
        {
            OnlineChatUi = null;
            ChatRoot = null;
            ChatTextTemplate = null;
            ChatText = null;
        }

        public string GetText()
        {
            if (!IsAvailable)
            {
                return string.Empty;
            }

            if (OnlineChatUI.currentMessage != null)
            {
                return OnlineChatUI.currentMessage;
            }

            return ChatText.text ?? string.Empty;
        }

        public void Clear()
        {
            SetText(string.Empty);
        }

        private GameObject FindDirectChild(GameObject parent, string childName)
        {
            if ((UnityEngine.Object)parent == null)
            {
                return null;
            }

            foreach (Transform child in parent.transform)
            {
                if (child.gameObject.name == childName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
