using ElementLogicFail.Scripts.Components.Pool;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace ElementLogicFail.Scripts.Systems.Pool
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PoolPrewarmSystem : ISystem
    {
        private BufferLookup<PooledEntity> _pooledEntityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            _pooledEntityLookup = state.GetBufferLookup<PooledEntity>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _pooledEntityLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var requestJob = new RequestPrefabLoadJob
            {
                ECB = ecb
            };
            state.Dependency = requestJob.ScheduleParallel(state.Dependency);

            var applyJob = new ApplyLoadedPrefabJob
            {
                ECB = ecb
            };
            state.Dependency = applyJob.ScheduleParallel(state.Dependency);

            var elementJob = new ElementPoolPrewarmJob
            {
                ECB = ecb,
                PooledEntityLookup = _pooledEntityLookup
            };
            state.Dependency = elementJob.ScheduleParallel(state.Dependency);

            var particleJob = new ParticlePoolPrewarmJob
            {
                ECB = ecb,
                PooledEntityLookup = _pooledEntityLookup
            };
            state.Dependency = particleJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(PrefabLoadResult))]
        [WithNone(typeof(RequestEntityPrefabLoaded))]
        public partial struct RequestPrefabLoadJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in ElementPool pool)
            {
                if (pool.Prefab == Entity.Null)
                {
                    ECB.AddComponent(sortKey, entity, new RequestEntityPrefabLoaded { Prefab = pool.PrefabReference });
                }
            }
        }

        [BurstCompile]
        public partial struct ApplyLoadedPrefabJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref ElementPool pool, in PrefabLoadResult prefabResult)
            {
                if (pool.Prefab == Entity.Null)
                {
                    pool.Prefab = prefabResult.PrefabRoot;
                }
            }
        }

        [BurstCompile]
        [WithNone(typeof(Prewarmed))]
        public partial struct ElementPoolPrewarmJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public BufferLookup<PooledEntity> PooledEntityLookup;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in ElementPool pool)
            {
                if (pool.Prefab == Entity.Null) return;

                if (!PooledEntityLookup.HasBuffer(entity))
                {
                    ECB.AddBuffer<PooledEntity>(sortKey, entity);
                }
                
                ECB.AddComponent<Prefab>(sortKey, pool.Prefab);

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
                if (pool.Prefab == Entity.Null) return;

                if (!PooledEntityLookup.HasBuffer(entity))
                {
                    ECB.AddBuffer<PooledEntity>(sortKey, entity);
                }
                
                ECB.AddComponent<Prefab>(sortKey, pool.Prefab);

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
