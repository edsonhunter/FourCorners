using ElementLogicFail.Scripts.Components.Request;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(MinionSpawningSystem))] // Explicitly guarantee order
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            // Using EndSimulation ECB allows us to process bases in parallel.
            // Spawns will technically execute 1 frame later when the ECB plays back,
            // which is highly performant and standard for DOTS networking.
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            uint seed = (uint)(SystemAPI.Time.ElapsedTime * 1000f) + 1;

            var job = new SpawnerJob 
            { 
                DeltaTime = deltaTime,
                Ecb = ecb,
                Seed = seed
            };
            
            // Now fully safe to run in parallel!
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
        public uint Seed;

        private void Execute(
            Entity entity,
            [EntityIndexInQuery] int sortKey,
            ref Components.Spawner.Spawner spawner,
            RefRO<LocalToWorld> worldTransform,
            DynamicBuffer<Components.Spawner.SpawnerPrefab> prefabs)
        {
            if (spawner.SpawnInterval <= 0f || spawner.SpawnAmount <= 0 || prefabs.Length <= 0 || !spawner.IsActive)
                return;

            spawner.Timer += DeltaTime;

            if (spawner.Timer >= spawner.SpawnInterval)
            {
                spawner.Timer -= spawner.SpawnInterval;
                
                // Create a unique random sequence for this exact spawner and frame
                var random = Unity.Mathematics.Random.CreateFromIndex(Seed ^ (uint)sortKey);

                for (int i = 0; i < spawner.SpawnAmount; i++)
                {
                    // Pick a random UnitModelType from the available prefabs this base supports
                    var randomPrefabIndex = random.NextInt(0, prefabs.Length);
                    var selectedType = prefabs[randomPrefabIndex].ModelType;

                    Ecb.AppendToBuffer(sortKey, entity, new MinionSpawnRequest
                    {
                        Type = spawner.Team,
                        ModelType = selectedType,
                        Position = worldTransform.ValueRO.Position
                    });
                }
            }
        }
    }
}