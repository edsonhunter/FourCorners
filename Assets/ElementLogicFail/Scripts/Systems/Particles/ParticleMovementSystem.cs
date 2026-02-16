using ElementLogicFail.Scripts.Components.Particles;
using ElementLogicFail.Scripts.Components.Pool;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Particles
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ParticleMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var endSimulationEntityCommandBufferSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>(); 
            var entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var deltaTime = SystemAPI.Time.DeltaTime;

            var job = new ParticleMovementJob
            {
                DeltaTime = deltaTime,
                Ecb = entityCommandBuffer,
                ParentPoolLookup = SystemAPI.GetComponentLookup<ParentPool>(true)
            };
            
            job.ParentPoolLookup.Update(ref state);
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct ParticleMovementJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public ComponentLookup<ParentPool> ParentPoolLookup;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref ParticleEffectData particle, ref LocalTransform transform)
        {
            particle.Timer += DeltaTime;
            if (particle.Timer >= particle.Lifetime)
            {
                if (ParentPoolLookup.HasComponent(entity))
                {
                    Ecb.AddComponent<ReturnToParticlePool>(sortKey, entity);
                }
                else
                {
                    Ecb.DestroyEntity(sortKey, entity);
                }
            }
            else
            {
                transform.Position.y += 2f * DeltaTime;
            }
        }
    }
}