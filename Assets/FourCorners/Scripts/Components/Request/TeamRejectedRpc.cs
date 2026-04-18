using Unity.NetCode;

namespace FourCorners.Scripts.Components.Request
{
    /// <summary>
    /// Sent from Server to the requesting Client when all 4 teams are already occupied
    /// and no fallback slot is available. The client UI listens for this RPC to surface
    /// an appropriate "lobby full" message.
    /// </summary>
    public struct TeamRejectedRpc : IRpcCommand { }
}
