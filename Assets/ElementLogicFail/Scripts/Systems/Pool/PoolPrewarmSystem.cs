using ElementLogicFail.Scripts.Components.Pool;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ElementLogicFail.Scripts.Systems.Pool
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PoolPrewarmSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var pooledEntityLookup = state.GetBufferLookup<PooledEntity>(true);

            var elementJob = new ElementPoolPrewarmJob
            {
                ECB = ecb,
                PooledEntityLookup = pooledEntityLookup
            };
            state.Dependency = elementJob.ScheduleParallel(state.Dependency);

            var particleJob = new ParticlePoolPrewarmJob
            {
                ECB = ecb,
                PooledEntityLookup = pooledEntityLookup
            };
            state.Dependency = particleJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(Prewarmed))]
        public partial struct ElementPoolPrewarmJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public BufferLookup<PooledEntity> PooledEntityLookup;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in ElementPool pool)
            {
                if (!PooledEntityLookup.HasBuffer(entity))
                {
                    ECB.AddBuffer<PooledEntity>(sortKey, entity);
                }
                
                for (int i = 0; i < pool.PoolSize; i++)
                {
                    var newInstance = ECB.Instantiate(sortKey, pool.Prefab);
                    ECB.AddComponent<Disabled>(sortKey, newInstance);
                    ECB.AddComponent(sortKey, newInstance, new SourcePool { PoolEntity = entity });
                    ECB.AppendToBuffer(sortKey, entity, new PooledEntity { Value = newInstance });
                }

                ECB.AddComponent<Prewarmed>(sortKey, entity);
            }
        }

        [BurstCompile]
        [WithNone(typeof(Prewarmed))]
        public partial struct ParticlePoolPrewarmJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public BufferLookup<PooledEntity> PooledEntityLookup;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in ParticlePool pool)
            {
                if (!PooledEntityLookup.HasBuffer(entity))
                {
                    ECB.AddBuffer<PooledEntity>(sortKey, entity);
                }
                
                for (int i = 0; i < pool.PoolSize; i++)
                {
                    var newInstance = ECB.Instantiate(sortKey, pool.Prefab);
                    ECB.AddComponent<Disabled>(sortKey, newInstance);
                    ECB.AddComponent(sortKey, newInstance, new ParentPool { PoolEntity = entity });
                    ECB.AppendToBuffer(sortKey, entity, new PooledEntity { Value = newInstance });
                }

                ECB.AddComponent<Prewarmed>(sortKey, entity);
            }
        }
    }
}
