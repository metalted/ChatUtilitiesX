using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ChatUtilities.Network
{
    [Serializable]
    public class HttpCommand
    {
        public string command;
        public string value;
        public bool send;
    }

    public class HttpCommandReceiver : IDisposable
    {
        public event Action<HttpCommand> CommandReceived;

        private readonly ConcurrentQueue<HttpCommand> commandQueue =
            new ConcurrentQueue<HttpCommand>();

        private readonly HttpListener listener;
        private readonly Thread listenerThread;

        private bool isRunning;

        public HttpCommandReceiver(int port)
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");

            isRunning = true;

            listener.Start();

            listenerThread = new Thread(ListenLoop);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        public void Update()
        {
            while (commandQueue.TryDequeue(out HttpCommand command))
            {
                CommandReceived?.Invoke(command);
            }
        }

        public void Dispose()
        {
            isRunning = false;

            listener.Stop();
            listener.Close();

            if (listenerThread.IsAlive)
            {
                listenerThread.Join(1000);
            }
        }

        private void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();

                    AddCorsHeaders(context.Response);

                    if (context.Request.HttpMethod == "OPTIONS")
                    {
                        context.Response.StatusCode = 204;
                        context.Response.Close();
                        continue;
                    }

                    if (context.Request.HttpMethod != "POST")
                    {
                        SendResponse(
                            context,
                            405,
                            "Only POST requests are supported.");

                        continue;
                    }

                    string json;

                    using (StreamReader reader = new StreamReader(
                        context.Request.InputStream,
                        context.Request.ContentEncoding))
                    {
                        json = reader.ReadToEnd();
                    }

                    HttpCommand command;

                    try
                    {
                        command = JsonUtility.FromJson<HttpCommand>(json);
                    }
                    catch
                    {
                        SendResponse(
                            context,
                            400,
                            "Invalid JSON.");

                        continue;
                    }

                    if (command == null ||
                        string.IsNullOrWhiteSpace(command.command))
                    {
                        SendResponse(
                            context,
                            400,
                            "Missing command.");

                        continue;
                    }

                    command.value ??= string.Empty;

                    commandQueue.Enqueue(command);

                    SendResponse(
                        context,
                        200,
                        "OK");
                }
                catch
                {
                    if (!isRunning)
                    {
                        return;
                    }
                }
            }
        }

        private void AddCorsHeaders(HttpListenerResponse response)
        {
            response.Headers.Add(
                "Access-Control-Allow-Origin",
                "*");

            response.Headers.Add(
                "Access-Control-Allow-Methods",
                "POST, OPTIONS");

            response.Headers.Add(
                "Access-Control-Allow-Headers",
                "Content-Type");
        }

        private void SendResponse(
            HttpListenerContext context,
            int statusCode,
            string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            AddCorsHeaders(context.Response);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = data.Length;

            context.Response.OutputStream.Write(
                data,
                0,
                data.Length);

            context.Response.Close();
        }
    }
}