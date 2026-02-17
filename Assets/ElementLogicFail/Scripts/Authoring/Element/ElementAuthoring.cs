using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using ElementLogicFail.Scripts.Components.Element;

namespace ElementLogicFail.Scripts.Authoring.Element
{
    public class ElementAuthoring : MonoBehaviour
    {
        public ElementType Type;
        public float speed;
        public int Cooldown;
        
        public class ElementBaker : Baker<ElementAuthoring>
        {
            public override void Bake(ElementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ElementData
                {
                    Type = authoring.Type,
                    Speed = authoring.speed,
                    Target = float3.zero,
                    RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue),
                    Cooldown = authoring.Cooldown,
                });
            }
        }
    }
}