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
                BaseSeed = seed
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
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
        public uint BaseSeed;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref Components.Spawner.Spawner spawner, RefRO<LocalTransform> transform, DynamicBuffer<Components.Spawner.SpawnerPrefab> prefabs)
        {
            var random = Random.CreateFromIndex(BaseSeed + (uint)sortKey);

            // Clamp rate to avoid memory explosion or divide by zero issues
            // Min 0.0f (paused), Max 50.0f
            spawner.SpawnRate = math.clamp(spawner.SpawnRate, 0f, 50f);

            spawner.Timer += DeltaTime;
            
            if (spawner.SpawnRate > 0.001f && prefabs.Length > 0)
            {
                float timePerSpawn = 1f / spawner.SpawnRate;
                if (spawner.Timer >= timePerSpawn)
                {
                    spawner.Timer = 0f;
                    
                    var prefabIndex = random.NextInt(0, prefabs.Length);
                    var modelType = prefabs[prefabIndex].ModelType;
                    
                    Ecb.AppendToBuffer(sortKey, entity, new ElementSpawnRequest
                    {
                        Type = spawner.Team,
                        Position = transform.ValueRO.Position,
                        ModelType = modelType
                    });
                }
            }
        }
    }
}