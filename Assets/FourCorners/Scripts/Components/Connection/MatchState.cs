using Unity.Entities;

namespace FourCorners.Scripts.Components.Connection
{
    /// <summary>
    /// Holds the current phase of the match. Stored on the MatchStateTag entity.
    /// Server is sole writer. Clients receive updates via LobbyStateUpdateRpc.
    /// </summary>
    public struct MatchState : IComponentData
    {
        public MatchPhase Phase;
    }
}
