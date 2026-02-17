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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var rateChanges = new NativeHashMap<int, float>(0, Allocator.TempJob);

            if (SystemAPI.TryGetSingletonBuffer<SpawnerRateChangeRequest>(out var requestBuffer))
            {
                if (requestBuffer.Length > 0)
                {
                    // Resize/Reallocate if needed, but since we created with 0, we must create new one or use capacity
                    rateChanges.Dispose(); // Dispose empty
                    rateChanges = new NativeHashMap<int, float>(requestBuffer.Length, Allocator.TempJob);
                    
                    foreach (var request in requestBuffer)
                    {
                        rateChanges[(int)request.Type] = request.NewRate;
                    }
                    requestBuffer.Clear();
                }
            }

            var seed = (uint)SystemAPI.Time.ElapsedTime + 1;
            
            //The source generator, which automatically writes the boilerplate code for IJobEntity,
            //sometimes fails when a job is initialized and scheduled in the same line.
            //It creates a unique key for the job based on the code text,
            //and if the initialization block is complex,
            //it can accidentally try to add the same key twice to its internal database.
            var job = new SpawnerJob
            {
                DeltaTime = deltaTime,
                Ecb = ecb,
                RateChanges = rateChanges,
                BaseSeed = seed
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);

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
        public uint BaseSeed;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref Components.Spawner.Spawner spawner, RefRO<LocalTransform> transform, DynamicBuffer<Components.Spawner.SpawnerPrefab> prefabs)
        {
            var random = Random.CreateFromIndex(BaseSeed + (uint)sortKey);

            if (RateChanges.TryGetValue((int)spawner.Type, out var newRate))
            {
                spawner.SpawnRate = newRate;
            }

            // Clamp rate to avoid memory explosion or divide by zero issues
            // Min 0.0f (paused), Max 20.0f (20 per sec is plenty for this demo)
            spawner.SpawnRate = math.clamp(spawner.SpawnRate, 0f, 50f);

            spawner.Timer += DeltaTime;
            
            if (spawner.SpawnRate > 0.001f && prefabs.Length > 0)
            {
                float timePerSpawn = 1f / spawner.SpawnRate;
                if (spawner.Timer >= timePerSpawn)
                {
                    spawner.Timer = 0f;
                    
                    var prefabIndex = random.NextInt(0, prefabs.Length);
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