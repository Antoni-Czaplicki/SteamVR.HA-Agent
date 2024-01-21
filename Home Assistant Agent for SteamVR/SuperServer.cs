using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket;
using SuperSocket.WebSocket.Server;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Home_Assistant_Agent_for_SteamVR
{
    class SuperServer
    {
        public enum ServerStatus
        {
            Connected,
            Disconnected,
            Error,
            ReceivedCount,
            DeliveredCount,
            SessionCount
        }

        private IHost host;
        private IServer _server;
        private readonly ConcurrentDictionary<string, WebSocketSession> _sessions = new ConcurrentDictionary<string, WebSocketSession>(); // Was getting crashes when loading all sessions from _server directly
        private volatile int _deliveredCount = 0;
        private volatile int _receivedCount = 0;

        #region Actions
        public Action<ServerStatus, int> StatusAction;
        public Action<WebSocketSession, string> MessageReceievedAction;
        public Action<WebSocketSession, byte[]> DataReceievedAction;
        public Action<WebSocketSession, bool, string> StatusMessageAction;
        #endregion

        public SuperServer(int port = 0)
        {
            ResetActions();
            if (port != 0) StartAsync(port);
        }

        #region Manage
        public async Task StartAsync(int port)
        {
            // Stop in case of already running
            Stop();

            var host = WebSocketHostBuilder.Create()
                .UseWebSocketMessageHandler(
                    async (session, message) =>
                    {
                        await session.SendAsync(message.Message);
                    }
                )
                .UseSessionHandler(async (s) =>
                {
                    _server = s.Server as IServer;
                })
                .ConfigureSuperSocket(options =>
                {
                    options.Name = "Echo Server";
                    options.AddListener(new ListenOptions
                    {
                        Ip = "Any",
                        Port = port
                    }
                    );
                    options.ReceiveBufferSize = 1024 * 1024;
                    options.MaxPackageLength = 1024 * 1024;
                })
                .ConfigureLogging((hostCtx, loggingBuilder) =>
                {
                    loggingBuilder.AddConsole();
                })
                .Build();

            await host.RunAsync();

        }

        public void Stop()
        {
            if (host != null)
            {
                host.Dispose();
                host.StopAsync();
            }
            StatusAction.Invoke(ServerStatus.Disconnected, 0);
        }

        public void ResetActions()
        {
            StatusAction = (status, value) =>
            {
                Debug.WriteLine($"SuperServer.StatusAction not set, missed status: {status} {value}");
            };
            MessageReceievedAction = (session, message) =>
            {
                Debug.WriteLine($"SuperServer.MessageReceivedAction not set, missed message: {message}");
            };
            DataReceievedAction = (session, data) =>
            {
                Debug.WriteLine($"SuperServer.DataReceivedAction not set, missed data: {data.Length}");
            };
            StatusMessageAction = (session, connected, message) =>
            {
                Debug.WriteLine($"SuperServer.StatusMessageAction not set, missed status: {connected} {message}");
            };
        }
        #endregion

        #region Listeners 
        private void Server_NewSessionConnected(WebSocketSession session)
        {
            _sessions[session.SessionID] = session;
            StatusMessageAction.Invoke(session, true, $"New session connected: {session.SessionID}");
            StatusAction(ServerStatus.SessionCount, _sessions.Count);
        }

        private void Server_NewMessageReceived(WebSocketSession session, string value)
        {
            MessageReceievedAction.Invoke(session, value);
            _receivedCount++;
            StatusAction(ServerStatus.ReceivedCount, _receivedCount);
        }

        private void Server_NewDataReceived(WebSocketSession session, byte[] value)
        {
            DataReceievedAction.Invoke(session, value);
        }

        private void Server_SessionClosed(WebSocketSession session, SuperSocket.Channel.CloseReason value)
        {
            _sessions.TryRemove(session.SessionID, out WebSocketSession oldSession);
            StatusMessageAction.Invoke(null, false, $"Session closed: {session.SessionID}");
            StatusAction(ServerStatus.SessionCount, _sessions.Count);
        }
        #endregion

        #region Send
        public void SendMessage(WebSocketSession session, string message)
        {
            if (_server.State != SuperSocket.ServerState.Started) return;
            if (session != null && session.State == SuperSocket.SessionState.Connected)
            {
                session.SendAsync(message);
                _deliveredCount++;
                StatusAction(ServerStatus.DeliveredCount, _deliveredCount);
            }
            else SendMessageToAll(message);
        }
        public void SendMessageToAll(string message)
        {
            foreach (var session in _sessions.Values)
            {
                if (session != null) SendMessage(session, message);
            }
        }
        #endregion
    }
}
