using Unity.Entities;

namespace FourCorners.Scripts.Components.Spawner
{
    public struct SpawnerRegistry : IComponentData
    {
        public Team.TeamNumber Type;
        public Entity SpawnerEntity;
    }
}
