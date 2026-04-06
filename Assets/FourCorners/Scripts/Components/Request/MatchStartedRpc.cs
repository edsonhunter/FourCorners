using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Request
{
    /// <summary>
    /// Broadcast RPC sent from the Server to ALL connected clients when
    /// the host successfully starts the game (MatchState transitions to Active).
    /// ClientMatchStartedSystem receives this and fires ISystemBridgeService.OnMatchStarted,
    /// which triggers every client's scene transition from Lobby → Gameplay.
    /// </summary>
    [GhostComponent]
    public struct MatchStartedRpc : IRpcCommand { }
}
