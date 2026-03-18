using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;

namespace ElementLogicFail.Scripts.Components.Element
{
    public struct ElementPrefabDescriptor : IComponentData
    {
        public int ElementType;
        public UnitModelType ModelType;
        public Unity.Entities.Serialization.EntityPrefabReference PrefabReference;
        public Entity Prefab;
        public int InitialCount;
    }
}