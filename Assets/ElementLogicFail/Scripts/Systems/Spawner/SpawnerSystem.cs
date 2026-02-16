using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Request;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Mathematics;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<Components.Spawner.Spawner>();
            state.RequireForUpdate<ElementSpawnRequest>();
            state.RequireForUpdate<SpawnerRateChangeRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var requestBuffer = SystemAPI.GetSingletonBuffer<SpawnerRateChangeRequest>();
            var rateChanges = new NativeHashMap<int, float>(requestBuffer.Length, Allocator.TempJob);
            
            foreach (var request in requestBuffer)
            {
                rateChanges[(int)request.Type] = request.NewRate;
            }
            requestBuffer.Clear();

            var seed = (uint)SystemAPI.Time.ElapsedTime + 1;
            
            state.Dependency = new SpawnerJob
            {
                DeltaTime = deltaTime,
                Ecb = ecb,
                RateChanges = rateChanges,
                Random = Random.CreateFromIndex(seed)
            }.ScheduleParallel(state.Dependency);

            rateChanges.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    [BurstCompile]
    public partial struct SpawnerJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeHashMap<int, float> RateChanges;
        public Random Random;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref Components.Spawner.Spawner spawner, RefRO<LocalTransform> transform, DynamicBuffer<Components.Spawner.SpawnerPrefab> prefabs)
        {
            if (RateChanges.TryGetValue((int)spawner.Type, out var newRate))
            {
                spawner.SpawnRate = newRate;
            }

            spawner.Timer += DeltaTime;
            
            if (spawner.SpawnRate > 0.001f && prefabs.Length > 0)
            {
                float timePerSpawn = 1f / spawner.SpawnRate;
                if (spawner.Timer >= timePerSpawn)
                {
                    spawner.Timer = 0f;
                    
                    var prefabIndex = Random.NextInt(0, prefabs.Length);
                    var prefabEntity = prefabs[prefabIndex].Prefab;
                    
                    Ecb.AppendToBuffer(sortKey, entity, new ElementSpawnRequest
                    {
                        Type = spawner.Type,
                        Position = transform.ValueRO.Position,
                        SpawnerEntity = entity,
                        PrefabToSpawn = prefabEntity
                    });
                }
            }
        }
    }
}