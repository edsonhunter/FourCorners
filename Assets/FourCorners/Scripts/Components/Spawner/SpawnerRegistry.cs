using FourCorners.Scripts.Components.Minion;
using Unity.Entities;

namespace FourCorners.Scripts.Components.Spawner
{
    public struct SpawnerRegistry : IComponentData
    {
        public Team Type;
        public Entity SpawnerEntity;
    }
}
