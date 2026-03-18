using ElementLogicFail.Scripts.Components.Bounds;
using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Pool;
using ElementLogicFail.Scripts.Components.Path;
using ElementLogicFail.Scripts.Components.Request;
using ElementLogicFail.Scripts.Systems.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpawnerSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ElementSpawningSystem : ISystem
    {
        private NativeParallelHashMap<int, Entity> _modelTypeToPrefab;
        private Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<WanderArea>();

            _modelTypeToPrefab = new NativeParallelHashMap<int, Entity>(16, Allocator.Persistent);
            _random = Random.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _modelTypeToPrefab.Clear();
            
            var area = SystemAPI.GetSingleton<WanderArea>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var buildMapJob = new BuildPrefabMapJob
            {
                ModelTypeToPrefab = _modelTypeToPrefab
            };
            state.Dependency = buildMapJob.Schedule(state.Dependency);

            var prefabLookup = SystemAPI.GetComponentLookup<ElementPrefabDescriptor>(true);
            var pathLookup = SystemAPI.GetBufferLookup<PathWaypoint>(true);
            var jobRandom = new Random(_random.NextUInt());

            var spawnJob = new ProcessSpawningJob
            {
                ModelTypeToPrefab = _modelTypeToPrefab,
                PrefabLookup = prefabLookup,
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
            if (_modelTypeToPrefab.IsCreated)
            {
                _modelTypeToPrefab.Dispose();
            }
        }
    }

    [BurstCompile]
    public partial struct BuildPrefabMapJob : IJobEntity
    {
        public NativeParallelHashMap<int, Entity> ModelTypeToPrefab;

        private void Execute(Entity entity, RefRO<ElementPrefabDescriptor> prefabDesc)
        {
            if (prefabDesc.ValueRO.ModelType != UnitModelType.None)
            {
                if (!ModelTypeToPrefab.ContainsKey((int)prefabDesc.ValueRO.ModelType))
                {
                    ModelTypeToPrefab.Add((int)prefabDesc.ValueRO.ModelType, entity);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct ProcessSpawningJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<int, Entity> ModelTypeToPrefab;
        [ReadOnly] public ComponentLookup<ElementPrefabDescriptor> PrefabLookup;
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

                if (ModelTypeToPrefab.TryGetValue((int)request.ModelType, out var prefabEntity))
                {
                    if (PrefabLookup.TryGetComponent(prefabEntity, out var prefabComponent))
                    {
                        if (prefabComponent.Prefab != Entity.Null)
                        {
                            Entity instance = Ecb.Instantiate(prefabComponent.Prefab);

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