using Unity.Entities;

namespace FourCorners.Scripts.Components.Spawner
{
    /// <summary>
    /// Tag added to a server-side connection entity after the GoInGameRequest RPC is received.
    /// Signals that this player still needs a base assigned, but we must wait for
    /// Netcode to activate the prespawned ghost bases (remove their Disabled tag) first.
    /// </summary>
    public struct PendingBaseAllocation : IComponentData { }
}
