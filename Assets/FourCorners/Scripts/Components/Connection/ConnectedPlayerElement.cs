using Unity.Entities;

namespace FourCorners.Scripts.Components.Connection
{
    /// <summary>
    /// Buffer element stored on the MatchStateTag entity.
    /// Each entry represents one successfully connected and accepted player.
    /// Used by the server to: (a) count players for the lobby start condition
    ///                        (b) broadcast LobbyStateUpdateRpc with an accurate PlayerCount.
    /// </summary>
    public struct ConnectedPlayerElement : IBufferElementData
    {
        /// <summary>NetworkId.Value of the connected player.</summary>
        public int NetworkId;

        /// <summary>The connection entity. Used to target RPCs to specific players.</summary>
        public Entity ConnectionEntity;
    }
}
