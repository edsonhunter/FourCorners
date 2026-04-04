using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using FourCorners.Scripts.Components.Minion;

namespace FourCorners.Scripts.Authoring.Minion
{
    public class MinionAuthoring : MonoBehaviour
    {
        public Team Type;
        public float speed;
        public int Cooldown;
        
        public class MinionBaker : Baker<MinionAuthoring>
        {
            public override void Bake(MinionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MinionData
                {
                    Team = authoring.Type,
                    Speed = authoring.speed,
                    Target = float3.zero,
                    RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue),
                    Cooldown = authoring.Cooldown,
                });
            }
        }
    }
}
