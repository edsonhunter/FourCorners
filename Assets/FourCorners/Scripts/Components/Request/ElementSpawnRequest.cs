using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;
using Unity.Mathematics;

namespace ElementLogicFail.Scripts.Components.Request
{
    public struct ElementSpawnRequest : IBufferElementData
    {
        public Team Type;
        public UnitModelType ModelType;
        public float3 Position;
    }
}