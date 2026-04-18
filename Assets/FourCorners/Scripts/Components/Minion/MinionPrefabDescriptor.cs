using Unity.Entities;

namespace FourCorners.Scripts.Components.Minion
{
    public struct MinionPrefabDescriptor : IComponentData
    {
        public UnitModelType ModelType;
        public Entity Prefab;
    }
}
