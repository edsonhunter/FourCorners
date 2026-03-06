using Unity.Entities;
using Unity.Mathematics;

namespace ElementLogicFail.Scripts.Components.Path
{
    public struct PathFollower : IComponentData
    {
        public int CurrentIndex;
        public bool IsReverse;
    }

    [InternalBufferCapacity(8)]
    public struct PathWaypoint : IBufferElementData
    {
        public float3 Position;
    }
}
