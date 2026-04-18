using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Spawner
{
    [GhostComponent]
    public struct SpawnerData : IComponentData
    {
        /// <summary>
        /// Baked link to the owning PlayerBase entity.
        /// All team identity and activation state is read from this entity via ComponentLookup.
        /// </summary>
        public Entity PlayerBaseEntity;

        public int SpawnAmount;
        public float SpawnInterval;
        public float Timer;

        /// <summary>
        /// Set by BaseAllocationSystem when the owning PlayerBase is activated.
        /// Kept as a [GhostField] so client-side systems can react to spawner activation
        /// without needing to traverse the PlayerBase hierarchy.
        /// Server-side activation logic uses PlayerBase.IsActive (via lookup) as the authority.
        /// </summary>
        [GhostField] public int NetworkId;
        [GhostField] public bool IsActive;
    }
}
