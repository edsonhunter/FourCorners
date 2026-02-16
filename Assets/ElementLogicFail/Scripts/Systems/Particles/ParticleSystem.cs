using ElementLogicFail.Scripts.Components.Particles;
using ElementLogicFail.Scripts.Components.Pool;
using ElementLogicFail.Scripts.Components.Request;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Particles
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ParticleSystem : ISystem
    {
        private NativeParallelHashMap<Entity, Entity> _prefabToPool;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _prefabToPool = new NativeParallelHashMap<Entity, Entity>(16, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _prefabToPool.Clear();
            
            EndSimulationEntityCommandBufferSystem.Singleton entitySimulationCommandBufferSystem =
                SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer entityCommandBuffer = entitySimulationCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);

            var buildJob = new BuildParticlePoolMapJob
            {
                PrefabToPoolMap = _prefabToPool
            };
            state.Dependency = buildJob.Schedule(state.Dependency);

            var spawnJob = new ProcessParticleSpawnJob
            {
                PrefabToPoolMap = _prefabToPool,
                PoolLookup = SystemAPI.GetBufferLookup<PooledEntity>(),
                Ecb = entityCommandBuffer
            };
            state.Dependency = spawnJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_prefabToPool.IsCreated)
            {
                _prefabToPool.Dispose();
            }
        }
    }

    [BurstCompile]
    public partial struct BuildParticlePoolMapJob : IJobEntity
    {
        public NativeParallelHashMap<Entity, Entity> PrefabToPoolMap;

        private void Execute(Entity entity, RefRO<ParticlePool> pool)
        {
            if (pool.ValueRO.Prefab != Entity.Null)
            {
                 // Schedule-single thread safety for HashMap or use ParallelWriter if changing to Parallel
                 // Since we use IJobEntity standard schedule (or parallel), HashMap needs parallel writer if multiple threads write.
                 // But multiple pools for SAME prefab? Unlikely.
                 // Checks:
                 if (!PrefabToPoolMap.ContainsKey(pool.ValueRO.Prefab))
                 {
                     PrefabToPoolMap.Add(pool.ValueRO.Prefab, entity);
                 }
            }
        }
    }

    [BurstCompile]
    public partial struct ProcessParticleSpawnJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<Entity, Entity> PrefabToPoolMap;
        public BufferLookup<PooledEntity> PoolLookup;
        public EntityCommandBuffer Ecb;

        private void Execute(DynamicBuffer<ParticleSpawnRequest> request)
        {
            foreach (var spawnRequest in request)
            {
                Entity instance;
                if (PrefabToPoolMap.TryGetValue(spawnRequest.Prefab, out Entity poolEntity))
                {
                    if (PoolLookup.TryGetBuffer(poolEntity, out var poolBuffer) && poolBuffer.Length > 0)
                    {
                        instance = poolBuffer[poolBuffer.Length - 1].Value;
                        poolBuffer.RemoveAt(poolBuffer.Length - 1);

                        Ecb.RemoveComponent<Disabled>(instance);
                        Ecb.SetComponent(instance, LocalTransform.FromPosition(spawnRequest.Position));
                        Ecb.SetComponent(instance, new ParticleEffectData { Lifetime = 1f, Timer = 0f });
                    }
                    else
                    {
                        instance = Ecb.Instantiate(spawnRequest.Prefab);
                        Ecb.AddComponent(instance, new ParentPool { PoolEntity = poolEntity });
                        Ecb.SetComponent(instance, LocalTransform.FromPosition(spawnRequest.Position));
                        Ecb.AddComponent(instance, new ParticleEffectData { Lifetime = 1f, Timer = 0f });
                    }
                }
                else
                {
                    instance = Ecb.Instantiate(spawnRequest.Prefab);
                    Ecb.SetComponent(instance, LocalTransform.FromPosition(spawnRequest.Position));
                    Ecb.AddComponent(instance, new ParticleEffectData { Lifetime = 1f, Timer = 0f });
                }
            }

            request.Clear();
        }
    }
}
