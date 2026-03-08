using ElementLogicFail.Scripts.Components.Bounds;
using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Pool;
using ElementLogicFail.Scripts.Components.Path;
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
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PoolSpawningSystem : ISystem
    {
        private NativeParallelHashMap<int, Entity> _modelTypeToPool;
        private Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<WanderArea>();

            _modelTypeToPool = new NativeParallelHashMap<int, Entity>(16, Allocator.Persistent);
            _random = Random.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _modelTypeToPool.Clear();
            
            var area = SystemAPI.GetSingleton<WanderArea>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var buildMapJob = new BuildPoolMapJob
            {
                ModelTypeToPool = _modelTypeToPool
            };
            state.Dependency = buildMapJob.Schedule(state.Dependency);

            var poolLookup = SystemAPI.GetComponentLookup<ElementPool>(true);
            var pathLookup = SystemAPI.GetBufferLookup<PathWaypoint>(true);
            var jobRandom = new Random(_random.NextUInt());

            var spawnJob = new ProcessSpawningJob
            {
                ModelTypeToPool = _modelTypeToPool,
                PoolLookup = poolLookup,
                PathLookup = pathLookup,
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
            if (_modelTypeToPool.IsCreated)
            {
                _modelTypeToPool.Dispose();
            }
        }
    }

    [BurstCompile]
    public partial struct BuildPoolMapJob : IJobEntity
    {
        public NativeParallelHashMap<int, Entity> ModelTypeToPool;

        private void Execute(Entity entity, RefRO<ElementPool> pool)
        {
            if (pool.ValueRO.ModelType != UnitModelType.None)
            {
                // NativeParallelHashMap is generally not safe for parallel writing unless using MultiHashMap or ParallelWriter.
                // Since we Schedule() this job and not Parallel, it is safe.
                if (!ModelTypeToPool.ContainsKey((int)pool.ValueRO.ModelType))
                {
                    ModelTypeToPool.Add((int)pool.ValueRO.ModelType, entity);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ProcessSpawningJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<int, Entity> ModelTypeToPool;
        [ReadOnly] public ComponentLookup<ElementPool> PoolLookup;
        [ReadOnly] public BufferLookup<PathWaypoint> PathLookup;
        public EntityCommandBuffer Ecb;
        public WanderArea Area;
        public Random Random;

        private void Execute(Entity spawnerEntity, DynamicBuffer<ElementSpawnRequest> requestBuffer, RefRO<Components.Spawner.Spawner> spawner)
        {
            if (requestBuffer.IsEmpty) return;

            for (int i = 0; i < requestBuffer.Length; i++)
            {
                var request = requestBuffer[i];
                if (request.Type != spawner.ValueRO.Team) continue;

                if (ModelTypeToPool.TryGetValue((int)request.ModelType, out var poolEntity))
                {
                    if (PoolLookup.TryGetComponent(poolEntity, out var poolComponent))
                    {
                        if (poolComponent.Prefab != Entity.Null)
                        {
                            Entity instance = Ecb.Instantiate(poolComponent.Prefab);

                            if (PathLookup.TryGetBuffer(spawnerEntity, out var spawnerPath))
                            {
                                var instancePath = Ecb.SetBuffer<PathWaypoint>(instance);
                                instancePath.AddRange(spawnerPath.AsNativeArray());
                                Ecb.SetComponent(instance, new PathFollower { CurrentIndex = 0 });
                            }

                            Ecb.SetComponent(instance, LocalTransform.FromPosition(request.Position));
                            Ecb.SetComponent(instance, new ElementData
                            {
                                Team = request.Type,
                                TeamColor = (TeamColor)request.Type,
                                Speed = 2f,
                                Target = new float3(
                                    Random.NextFloat(Area.MinArea.x, Area.MaxArea.x),
                                    0,
                                    Random.NextFloat(Area.MinArea.z, Area.MaxArea.z)),
                                RandomSeed = Random.NextUInt(),
                                Cooldown = 2f
                            });
                        }
                    }
                }
            }
            requestBuffer.Clear();
        }
    }
}