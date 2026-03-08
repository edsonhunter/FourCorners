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

            // Only request Addressable loading when PrefabReference is explicitly valid.
            // Pools whose Prefab was baked directly (PrefabReference == default) skip this entirely.
            var requestJob = new RequestPrefabLoadJob { ECB = ecb };
            state.Dependency = requestJob.ScheduleParallel(state.Dependency);

            var applyJob = new ApplyLoadedPrefabJob { ECB = ecb };
            state.Dependency = applyJob.ScheduleParallel(state.Dependency);

            // Only particle pools are pre-warmed (instantiating disabled copies upfront).
            // Minion (Ghost) entities are managed by Netcode -- never pre-warm them here.
            var particleJob = new ParticlePoolPrewarmJob
            {
                ECB = ecb,
                PooledEntityLookup = _pooledEntityLookup
            };
            state.Dependency = particleJob.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Requests Addressable loading ONLY when there is no directly-baked prefab AND a valid PrefabReference exists.
        /// </summary>
        [BurstCompile]
        [WithNone(typeof(PrefabLoadResult))]
        [WithNone(typeof(RequestEntityPrefabLoaded))]
        public partial struct RequestPrefabLoadJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in ElementPool pool)
            {
                if (pool.Prefab == Entity.Null && pool.PrefabReference.IsReferenceValid)
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

        /// <summary>
        /// Pre-warms ONLY particle pools by pre-instantiating disabled copies.
        /// Do NOT pre-warm Minion Ghost entities — Netcode owns their lifecycle.
        /// </summary>
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
