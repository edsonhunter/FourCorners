using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Request
{
    /// <summary>
    /// RPC sent from the Client to the Server AFTER transitioning to the GameplayScene
    /// and successfully loading all referenced SubScenes.
    ///
    /// Triggers ServerStreamReadySystem to append NetworkStreamInGame and PendingBaseAllocation,
    /// allowing Ghost synchronization to commence only when the client is truly ready.
    /// </summary>
    [GhostComponent]
    public struct ReadyForGhostsRequest : IRpcCommand { }
}
