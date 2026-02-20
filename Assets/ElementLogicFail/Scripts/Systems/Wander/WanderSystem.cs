using ElementLogicFail.Scripts.Components.Bounds;
using ElementLogicFail.Scripts.Components.Element;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace ElementLogicFail.Scripts.Systems.Wander
{
    [BurstCompile]
    public partial struct WanderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WanderArea>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var area = SystemAPI.GetSingleton<WanderArea>();

            var job = new WanderJob
            {
                DeltaTime = deltaTime,
                Area = area
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    [BurstCompile]
    [WithNone(typeof(ElementLogicFail.Scripts.Components.Path.PathFollower))]
    public partial struct WanderJob : IJobEntity
    {
        public float DeltaTime;
        public WanderArea Area;

        private void Execute(ref ElementData element, ref LocalTransform transform)
        {
            if (math.distance(transform.Position, element.Target) < 0.2f)
            {
                element.RandomSeed = element.RandomSeed * 1664525u + 1013904223u;
                var rand = new Unity.Mathematics.Random(element.RandomSeed);
                element.Target = new float3(
                    rand.NextFloat(Area.MinArea.x, Area.MaxArea.x),
                    0,
                    rand.NextFloat(Area.MinArea.z, Area.MaxArea.z));
                element.RandomSeed = rand.NextUInt();
            }
            
            float3 direction = math.normalizesafe(element.Target - transform.Position);
            
            // Apply rotation to face movement direction
            if (math.lengthsq(direction) > 0.001f)
            {
                transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
            }

            transform.Position += direction * element.Speed * DeltaTime;
        }
    }
}