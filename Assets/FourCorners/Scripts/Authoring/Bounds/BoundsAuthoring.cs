using Unity.Entities;
using UnityEngine;
using ElementLogicFail.Scripts.Components.Bounds;

namespace ElementLogicFail.Scripts.Authoring.Bounds
{
    public class BoundsAuthoring : MonoBehaviour
    {
        public Vector3 min = new Vector3(-10f, 0f, -10f);
        public Vector3 max = new Vector3(10f, 0f, 10f);
        
        public class BoundsBaker : Baker<BoundsAuthoring>
        {
            public override void Bake(BoundsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new WanderArea
                {
                    MinArea = authoring.min,
                    MaxArea = authoring.max
                });
            }
        }
    }
}