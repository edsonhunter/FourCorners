using ElementLogicFail.Scripts.Components.Minion;
using Unity.Entities;
using Unity.Mathematics;

namespace ElementLogicFail.Scripts.Components.Request
{
    public struct MinionSpawnRequest : IBufferElementData
    {
        public Team Type;
        public UnitModelType ModelType;
        public float3 Position;
    }
}