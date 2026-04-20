using Unity.NetCode;

namespace FourCorners.Scripts.Components.Request
{
    /// <summary>
    /// RPC sent from the Client to the Server AFTER transitioning to the GameplayScene
    /// and successfully loading all referenced SubScenes.
    ///
    /// Triggers ServerStreamReadySystem to append NetworkStreamInGame and PendingBaseAllocation,
    /// allowing Ghost synchronization to commence only when the client is truly ready.
    ///
    /// NOTE: IRpcCommand structs must NOT carry [GhostComponent]. Ghost replication and the
    /// reliable RPC command stream are mutually exclusive pipelines. Adding [GhostComponent]
    /// causes the codegen to emit a Ghost serializer for this type, which conflicts with the
    /// RPC serializer and silently drops the payload for remote (non-IPC) transports.
    /// </summary>
    public struct ReadyForGhostsRequest : IRpcCommand { }
}
