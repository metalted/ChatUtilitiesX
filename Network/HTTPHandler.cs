using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities.Network
{
    public class HTTPHandler
    {
        private readonly int port;
        private HttpCommandReceiver commandReceiver;

        private readonly Queue<float> recentSendTimes = new Queue<float>();

        private const float MinimumSendInterval = 1f;
        private const float RateLimitWindow = 10f;
        private const int MaximumSendsPerWindow = 5;

        private float lastSendTime = float.NegativeInfinity;

        public HTTPHandler(int port)
        {
            this.port = port;

            commandReceiver = new HttpCommandReceiver(port);
            commandReceiver.CommandReceived += OnCommandReceived;
        }

        public void Update()
        {
            commandReceiver?.Update();
        }

        public void OnDestroy()
        {
            if (commandReceiver == null)
            {
                return;
            }

            commandReceiver.CommandReceived -= OnCommandReceived;
            commandReceiver.Dispose();
            commandReceiver = null;
        }

        private void OnCommandReceived(HttpCommand command)
        {
            if (Plugin.Instance.chatInput == null ||
                !Plugin.Instance.chatInput.IsAvailable)
            {
                Debug.LogWarning(
                    "Chat input is not available. Cannot process command.");

                return;
            }

            if (command == null ||
                string.IsNullOrWhiteSpace(command.command))
            {
                Debug.LogWarning("Received an invalid HTTP command.");
                return;
            }

            switch (command.command.ToLowerInvariant())
            {
                case "message":
                    {
                        HandleMessageCommand(command);
                        break;
                    }

                default:
                    {
                        Debug.LogWarning(
                            $"Unknown HTTP command: {command.command}");

                        break;
                    }
            }
        }

        private void HandleMessageCommand(HttpCommand command)
        {
            string value = command.value ?? string.Empty;

            Plugin.Instance.chatInput.SetText(value);

            if (!command.send)
            {
                return;
            }

            if (!CanSendMessage())
            {
                Debug.LogWarning(
                    "HTTP chat message blocked by the rate limiter.");

                return;
            }

            RegisterMessageSend();

            Plugin.Instance.chatInput.Send();
        }

        private bool CanSendMessage()
        {
            float currentTime = Time.realtimeSinceStartup;

            RemoveExpiredSendTimes(currentTime);

            if (currentTime - lastSendTime < MinimumSendInterval)
            {
                return false;
            }

            if (recentSendTimes.Count >= MaximumSendsPerWindow)
            {
                return false;
            }

            return true;
        }

        private void RegisterMessageSend()
        {
            float currentTime = Time.realtimeSinceStartup;

            lastSendTime = currentTime;
            recentSendTimes.Enqueue(currentTime);
        }

        private void RemoveExpiredSendTimes(float currentTime)
        {
            while (recentSendTimes.Count > 0 &&
                   currentTime - recentSendTimes.Peek() >= RateLimitWindow)
            {
                recentSendTimes.Dequeue();
            }
        }
    }
}