using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Request
{
    /// <summary>
    /// Sent from the Server to a specific Client whenever the lobby state changes:
    /// a new player joins, or a player disconnects.
    /// The receiving ClientLobbyStateSystem uses these values to drive the lobby UI
    /// without needing a replicated singleton.
    /// </summary>
    [GhostComponent]
    public struct LobbyStateUpdateRpc : IRpcCommand
    {
        /// <summary>True if the receiving client is the designated host of this lobby.</summary>
        public bool IsHost;

        /// <summary>Number of players currently confirmed in the lobby (accepted by server).</summary>
        public int PlayerCount;
    }
}
