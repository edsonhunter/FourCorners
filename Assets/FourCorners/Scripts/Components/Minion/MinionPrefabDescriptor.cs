using Unity.Entities;

namespace FourCorners.Scripts.Components.Minion
{
    public struct MinionPrefabDescriptor : IComponentData
    {
        public int MinionType;
        public UnitModelType ModelType;
        public Unity.Entities.Serialization.EntityPrefabReference PrefabReference;
        public Entity Prefab;
        public int InitialCount;
    }
}
