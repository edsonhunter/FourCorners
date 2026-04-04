using Unity.Entities;

namespace FourCorners.Scripts.Components.Spawner
{
    public struct SpawnControl : IComponentData
    {
        public float SpawnRateMultiplier;
    }
}
