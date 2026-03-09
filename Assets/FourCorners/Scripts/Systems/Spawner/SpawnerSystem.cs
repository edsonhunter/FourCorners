using ElementLogicFail.Scripts.Components.Request;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Require at least one spawner entity with SpawnerPrefab;
            // ElementSpawnRequest is a DynamicBuffer and must NOT be used in RequireForUpdate.
            var builder = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithAll<Components.Spawner.Spawner, Components.Spawner.SpawnerPrefab>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Write spawn requests DIRECTLY into the DynamicBuffer — NOT via ECB.
            // If we used an ECB with EndSimulationEntityCommandBufferSystem, the buffer
            // would only get populated AFTER PoolSpawningSystem already ran (same frame),
            // causing PoolSpawningSystem to always read an empty buffer.
            var job = new SpawnerJob { DeltaTime = deltaTime };
            // Use Schedule (not ScheduleParallel) — DynamicBuffer write with ScheduleParallel
            // can silently fail safety checks when ref DynamicBuffer is involved.
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [BurstCompile]
    public partial struct SpawnerJob : IJobEntity
    {
        public float DeltaTime;

        private void Execute(
            ref Components.Spawner.Spawner spawner,
            RefRO<LocalTransform> transform,
            DynamicBuffer<Components.Spawner.SpawnerPrefab> prefabs,
            ref DynamicBuffer<ElementSpawnRequest> spawnRequests)
        {
            if (spawner.SpawnInterval <= 0f || spawner.SpawnAmount <= 0 || prefabs.Length <= 0 || !spawner.IsActive)
                return;

            spawner.Timer += DeltaTime;

            if (spawner.Timer >= spawner.SpawnInterval)
            {
                spawner.Timer -= spawner.SpawnInterval;

                var prefabType = prefabs[0].ModelType;
                for (int i = 0; i < spawner.SpawnAmount; i++)
                {
                    spawnRequests.Add(new ElementSpawnRequest
                    {
                        Type = spawner.Team,
                        ModelType = prefabType,
                        Position = transform.ValueRO.Position
                    });
                }
            }
        }
    }
}