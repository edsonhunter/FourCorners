using Unity.Entities;

namespace FourCorners.Scripts.Components.Connection
{
    /// <summary>
    /// Client-side tag added to the NetworkId connection entity once GoInGameRequest
    /// has been sent. This prevents ClientRequestGameSystem from sending the request repeatedly.
    /// Ghost streaming (NetworkStreamInGame) is deferred until the subscenes are loaded.
    /// </summary>
    public struct ClientLobbyJoinedTag : IComponentData { }
}
