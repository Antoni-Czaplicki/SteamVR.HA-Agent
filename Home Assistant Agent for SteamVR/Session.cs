using SuperSocket.WebSocket.Server;
using System.Collections.Concurrent;

namespace Home_Assistant_Agent_for_SteamVR
{
    class Session
    {
        public readonly static ConcurrentDictionary<string, WebSocketSession> Sessions = new ConcurrentDictionary<string, WebSocketSession>();
        public readonly static ConcurrentDictionary<WebSocketSession, ulong> OverlayHandles = new ConcurrentDictionary<WebSocketSession, ulong>();
    }
}
