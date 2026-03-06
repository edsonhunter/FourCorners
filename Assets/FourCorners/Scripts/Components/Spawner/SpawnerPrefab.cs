using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;

namespace ElementLogicFail.Scripts.Components.Spawner
{
    [InternalBufferCapacity(8)]
    public struct SpawnerPrefab : IBufferElementData
    {
        public UnitModelType ModelType;
    }
}
