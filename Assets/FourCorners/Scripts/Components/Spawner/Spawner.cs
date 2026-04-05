using FourCorners.Scripts.Components.Minion;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Spawner
{
    public struct SpawnerData : IComponentData
    {
        public Team.TeamNumber TeamNumber;
        public int SpawnAmount;
        public float SpawnInterval;
        public float Timer;
        [GhostField] public int NetworkId;
        [GhostField] public bool IsActive;
    }
}
