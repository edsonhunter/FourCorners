using ElementLogicFail.Scripts.Components.Pool;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ElementLogicFail.Scripts.Systems.Particles
{
    public partial struct ReturnParticleToPoolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new ReturnParticleJob
            {
                Ecb = ecb
            };
            
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct ReturnParticleJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRO<ParentPool> pool, RefRO<ReturnToParticlePool> returnTag)
        {
            Ecb.AddComponent<Disabled>(sortKey, entity);
            Ecb.AppendToBuffer(sortKey, pool.ValueRO.PoolEntity, new PooledEntity { Value = entity });
            Ecb.RemoveComponent<ReturnToParticlePool>(sortKey, entity);
        }
    }
}