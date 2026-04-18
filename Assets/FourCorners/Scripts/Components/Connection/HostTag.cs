using Unity.Entities;

namespace FourCorners.Scripts.Components.Connection
{
    /// <summary>
    /// Added to the NetworkStreamConnection entity of the very first player accepted by the server.
    /// The HostStartGameSystem checks for this tag to authorize the StartGameRequest RPC.
    /// Only one entity in the world should ever carry this tag during a match.
    /// </summary>
    public struct HostTag : IComponentData { }
}
