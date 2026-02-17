using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;
using Unity.Mathematics;

namespace ElementLogicFail.Scripts.Components.Request
{
    public struct ElementSpawnRequest : IBufferElementData
    {
        public ElementType Type;
        public UnitModelType ModelType;
        public float3 Position;
    }
}