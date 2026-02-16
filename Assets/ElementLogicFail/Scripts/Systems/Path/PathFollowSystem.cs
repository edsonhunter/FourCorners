using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Path;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Path
{
    [BurstCompile]
    public partial struct PathFollowSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, follower, element, buffer) in 
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<PathFollower>, RefRW<ElementData>, DynamicBuffer<PathWaypoint>>())
            {
                if (buffer.IsEmpty) continue;

                var followerRW = follower.ValueRW;
                var currentTarget = buffer[followerRW.CurrentIndex].Position;
                
                element.ValueRW.Target = currentTarget;
                // Move towards target
                float3 direction = math.normalizesafe(currentTarget - transform.ValueRO.Position);
                // Zero out Y movement if purely 2D plane logic is desired, but keeping 3D generally safe
                direction.y = 0; 
                
                transform.ValueRW.Position += direction * element.ValueRO.Speed * deltaTime;

                // Check distance
                if (math.distancesq(transform.ValueRO.Position, currentTarget) < 0.2f * 0.2f)
                {
                    // Reached waypoint
                    followerRW.CurrentIndex++;
                    if (followerRW.CurrentIndex >= buffer.Length)
                    {
                        followerRW.CurrentIndex = 0;
                    }
                }
                
                follower.ValueRW = followerRW;
            }
        }
    }
}
