using ElementLogicFail.Scripts.Components.Minion;
using Unity.Entities;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Components.Spawner
{
    public struct Spawner : IComponentData
    {
        public Team Team;
        public int SpawnAmount;
        public float SpawnInterval;
        public float Timer;
        [GhostField] public int NetworkId;
        [GhostField] public bool IsActive;
    }
}