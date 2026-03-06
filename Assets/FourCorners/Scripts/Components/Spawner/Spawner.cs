using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;

namespace ElementLogicFail.Scripts.Components.Spawner
{
    public struct Spawner : IComponentData
    {
        public Team Team;
        public int SpawnAmount;
        public float SpawnInterval;
        public float Timer;
    }
}