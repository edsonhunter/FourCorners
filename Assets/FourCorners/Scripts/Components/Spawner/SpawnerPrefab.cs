using FourCorners.Scripts.Components.Minion;
using Unity.Entities;

namespace FourCorners.Scripts.Components.Spawner
{
    [InternalBufferCapacity(8)]
    public struct SpawnerPrefab : IBufferElementData
    {
        public UnitModelType ModelType;
    }
}
