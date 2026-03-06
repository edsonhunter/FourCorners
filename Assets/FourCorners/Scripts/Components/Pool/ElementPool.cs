using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;

namespace ElementLogicFail.Scripts.Components.Pool
{
    public struct ElementPool : IComponentData
    {
        public int ElementType;
        public UnitModelType ModelType;
        public Unity.Entities.Serialization.EntityPrefabReference PrefabReference;
        public Entity Prefab;
        public int PoolSize;
    }
}