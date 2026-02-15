using ElementLogicFail.Scripts.Components.Bounds;
using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Pool;
using ElementLogicFail.Scripts.Components.Request;
using ElementLogicFail.Scripts.Systems.Collision;
using ElementLogicFail.Scripts.Systems.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Pool
{
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(CollisionSystem))]
    [UpdateAfter(typeof(SpawnerSystem))]
    public partial struct PoolSpawningSystem : ISystem
    {
        private NativeParallelHashMap<Entity, Entity> _prefabToPool;
        private Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<WanderArea>();

            _prefabToPool = new NativeParallelHashMap<Entity, Entity>(16, Allocator.Persistent);
            _random = Random.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _prefabToPool.Clear();
            
            var area = SystemAPI.GetSingleton<WanderArea>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var buildMapJob = new BuildPoolMapJob
            {
                PrefabToPool = _prefabToPool
            };
            state.Dependency = buildMapJob.Schedule(state.Dependency);

            var poolLookup = SystemAPI.GetBufferLookup<PooledEntity>();
            var jobRandom = new Random(_random.NextUInt());

            var spawnJob = new ProcessSpawningJob
            {
                PrefabToPool = _prefabToPool,
                PoolLookup = poolLookup,
                Ecb = ecb,
                Area = area,
                Random = jobRandom
            };
            
            state.Dependency = spawnJob.Schedule(state.Dependency);
            
            _random.NextUInt(); 
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
    public partial struct BuildPoolMapJob : IJobEntity
    {
        public NativeParallelHashMap<Entity, Entity> PrefabToPool;

        private void Execute(Entity entity, RefRO<ElementPool> pool)
        {
            if (pool.ValueRO.Prefab != Entity.Null)
            {
                // NativeParallelHashMap is generally not safe for parallel writing unless using MultiHashMap or ParallelWriter.
                // Since we Schedule() this job and not Parallel, it is safe.
                if (!PrefabToPool.ContainsKey(pool.ValueRO.Prefab))
                {
                    PrefabToPool.Add(pool.ValueRO.Prefab, entity);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ProcessSpawningJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<Entity, Entity> PrefabToPool;
        public BufferLookup<PooledEntity> PoolLookup;
        public EntityCommandBuffer Ecb;
        public WanderArea Area;
        public Random Random;

        private void Execute(Entity spawnerEntity, DynamicBuffer<ElementSpawnRequest> requestBuffer, RefRO<Components.Spawner.Spawner> spawner)
        {
            if (requestBuffer.IsEmpty) return;

            for (int i = 0; i < requestBuffer.Length; i++)
            {
                var request = requestBuffer[i];
                if (request.Type != spawner.ValueRO.Type) continue;

                if (PrefabToPool.TryGetValue(spawner.ValueRO.ElementPrefab, out var poolEntity))
                {
                    if (PoolLookup.TryGetBuffer(poolEntity, out var pooledBuffer))
                    {
                        if (pooledBuffer.Length > 0)
                        {
                            // Pop from pool
                            Entity instance = pooledBuffer[pooledBuffer.Length - 1].Value;
                            pooledBuffer.RemoveAt(pooledBuffer.Length - 1);

                            Ecb.SetComponent(instance, LocalTransform.FromPosition(request.Position));
                            Ecb.SetComponent(instance, new ElementData
                            {
                                Type = request.Type,
                                Speed = 2f,
                                Target = new float3(
                                    Random.NextFloat(Area.MinArea.x, Area.MaxArea.x),
                                    0,
                                    Random.NextFloat(Area.MinArea.z, Area.MaxArea.z)),
                                RandomSeed = Random.NextUInt(),
                                Cooldown = 2f
                            });

                            Ecb.RemoveComponent<Disabled>(instance);
                        }
                    }
                }
            }
            requestBuffer.Clear();
        }
    }
}