using Unity.Entities;

namespace FourCorners.Scripts.Components.Team
{
    public struct TeamStatusElement : IBufferElementData
    {
        public bool IsOccupied;
        public Entity OccupyingPlayer; // Reference to the NetworkId entity
    }

// Tag to find the entity without a Singleton pattern
    public struct MatchStateTag : IComponentData
    {
    }
}