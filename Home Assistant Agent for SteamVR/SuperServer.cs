using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket;
using SuperSocket.WebSocket.Server;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Home_Assistant_Agent_for_SteamVR
{
    public class SuperServer
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

        public ListenOptions LServerOptions = new ListenOptions
        {
            Ip = "Any",
            Port = 8077
        };

        private readonly ConcurrentDictionary<string, WebSocketSession> _sessions =
            new ConcurrentDictionary<string, WebSocketSession>(); // Was getting crashes when loading all sessions from _server directly

        #region Actions

        public Action<WebSocketSession, int> SessionHandler;
        public Func<WebSocketSession, string, Task> MessageReceievedAction;
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
            await StopAsync();

            LServerOptions.Port = port;

            host = WebSocketHostBuilder.Create()
                .UseWebSocketMessageHandler(
                    async (session, message) =>
                    {
                        await Server_NewMessageReceived(session, message.Message);
                    }
                )
                .UseSessionHandler(async (s) => { await Server_NewSessionConnected(s as WebSocketSession); },
                    async (s, e) =>
                    {
                        // s: the session
                        // e: the CloseEventArgs
                        // e.Reason: the close reason
                        // things to do after the session closes
                        await Server_SessionClosedAsync(s as WebSocketSession, e.Reason);
                    })
                .ConfigureSuperSocket(options =>
                {
                    options.Name = "Home Assistant Agent for SteamVR";
                    options.AddListener(LServerOptions);
                    options.ReceiveBufferSize = 1024 * 1024;
                    options.MaxPackageLength = 1024 * 1024;
                })
                .UseInProcSessionContainer()
                .ConfigureLogging((hostCtx, loggingBuilder) => { loggingBuilder.AddConsole(); })
                .Build();

            await host.RunAsync();
        }

        public async Task StopAsync()
        {
            if (host != null)
            {
                foreach (var service in host.Services.GetServices<IHostedService>())
                {
                    if (service is IServer server)
                    {
                        var container = server.GetSessionContainer();

                        var serverSessions = container?.GetSessions();
                        if (serverSessions == null) continue;
                        foreach (var session in serverSessions)
                        {
                            try
                            {
                                await session.CloseAsync(SuperSocket.Channel.CloseReason.ServerShutdown);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error closing session: {ex.Message}");
                            }
                        }
                    }
                }

                await host.StopAsync();
            }
        }

        public async Task RestartAsync(int port)
        {
            await StopAsync();
            await StartAsync(port);
        }

        public ServerState GetState()
        {
            if (host == null) return ServerState.None;
            try
            {
                foreach (var service in host.Services.GetServices<IHostedService>())
                {
                    if (service is IServer server)
                    {
                        return server.State;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }

            return ServerState.None;
        }

        public void ResetActions()
        {
            SessionHandler = (status, value) =>
            {
                Debug.WriteLine($"SuperServer.StatusAction not set, missed status: {status} {value}");
            };
            MessageReceievedAction = (session, message) =>
            {
                Debug.WriteLine($"SuperServer.MessageReceivedAction not set, missed message: {message}");
                return Task.CompletedTask;
            };
            StatusMessageAction = (session, connected, message) =>
            {
                Debug.WriteLine($"SuperServer.StatusMessageAction not set, missed status: {connected} {message}");
            };
        }

        #endregion

        #region Listeners

        private async Task Server_NewSessionConnected(WebSocketSession session)
        {
            _sessions[session.SessionID] = session;
            StatusMessageAction.Invoke(session, true, $"New session connected: {session.SessionID}");
            SessionHandler(session, _sessions.Count);
        }

        private async Task Server_NewMessageReceived(WebSocketSession session, string value)
        {
            await MessageReceievedAction.Invoke(session, value);
        }


        private async Task Server_SessionClosedAsync(WebSocketSession session, SuperSocket.Channel.CloseReason value)
        {
            _sessions.TryRemove(session.SessionID, out WebSocketSession oldSession);
            StatusMessageAction.Invoke(null, false, $"Session closed: {session.SessionID}");
            SessionHandler(session, _sessions.Count);
        }

        #endregion

        #region Send

        public void SendMessage(WebSocketSession session, string message)
        {
            if (session.Server.State != ServerState.Started) return;
            if (session != null && session.State == SessionState.Connected)
            {
                session.SendAsync(message);
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