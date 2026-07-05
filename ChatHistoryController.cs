using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities
{
    public class ChatHistoryController
    {
        private ChatInputController chatInput;
        private List<string> messages = new List<string>();
        private int selectedIndex;
        private bool justSelectedHistory;
        private string justSelectedHistoryText = string.Empty;

        public int Count
        {
            get
            {
                return messages.Count;
            }
        }

        public void BindInput(ChatInputController newChatInput)
        {
            chatInput = newChatInput;
            selectedIndex = messages.Count;
            justSelectedHistory = false;
            justSelectedHistoryText = string.Empty;
        }

        public void Add(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (messages.Count > 0)
            {
                string previousMessage = messages[messages.Count - 1];

                if (previousMessage == message)
                {
                    selectedIndex = messages.Count;
                    justSelectedHistory = false;
                    justSelectedHistoryText = string.Empty;
                    return;
                }
            }

            messages.Add(message);
            selectedIndex = messages.Count;
            justSelectedHistory = false;
            justSelectedHistoryText = string.Empty;
        }

        public void Update()
        {
            if (chatInput == null || !chatInput.IsAvailable)
            {
                return;
            }

            UpdateManualEditState();

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MovePrevious();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveNext();
                return;
            }
        }

        private void UpdateManualEditState()
        {
            if (!justSelectedHistory)
            {
                return;
            }

            if (chatInput.GetText() != justSelectedHistoryText)
            {
                justSelectedHistory = false;
                justSelectedHistoryText = string.Empty;
                selectedIndex = messages.Count;
            }
        }

        private void MovePrevious()
        {
            if (messages.Count == 0)
            {
                return;
            }

            if (selectedIndex <= 0)
            {
                return;
            }

            selectedIndex--;
            ApplySelectedHistory();
        }

        private void MoveNext()
        {
            if (messages.Count == 0)
            {
                return;
            }

            if (selectedIndex >= messages.Count - 1)
            {
                return;
            }

            selectedIndex++;
            ApplySelectedHistory();
        }

        private void ApplySelectedHistory()
        {
            if (selectedIndex < 0 || selectedIndex >= messages.Count)
            {
                return;
            }

            justSelectedHistory = true;
            justSelectedHistoryText = messages[selectedIndex];
            chatInput.SetText(justSelectedHistoryText);
        }
    }
}
