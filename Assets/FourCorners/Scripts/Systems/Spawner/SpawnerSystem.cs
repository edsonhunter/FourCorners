using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Components.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace FourCorners.Scripts.Systems.Spawner
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(MinionSpawningSystem))]
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
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Read-only lookup: spawners check their owning base's IsActive flag.
            // This keeps TeamNumber exclusively on PlayerBase — no duplication in SpawnerData.
            var playerBaseLookup = SystemAPI.GetComponentLookup<PlayerBase>(isReadOnly: true);

            var job = new SpawnerJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb,
                Seed = (uint)(SystemAPI.Time.ElapsedTime * 1000f) + 1,
                PlayerBaseLookup = playerBaseLookup
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
        public uint Seed;

        [ReadOnly] public ComponentLookup<PlayerBase> PlayerBaseLookup;

        private void Execute(
            Entity entity,
            [EntityIndexInQuery] int sortKey,
            ref SpawnerData spawner,
            RefRO<LocalToWorld> worldTransform,
            DynamicBuffer<SpawnerPrefab> prefabs)
        {
            if (spawner.SpawnInterval <= 0f || spawner.SpawnAmount <= 0 || prefabs.Length <= 0)
                return;

            // Authority check: read IsActive from the owning PlayerBase
            if (!PlayerBaseLookup.TryGetComponent(spawner.PlayerBaseEntity, out var owningBase) || !owningBase.IsActive)
                return;

            spawner.Timer += DeltaTime;

            if (spawner.Timer >= spawner.SpawnInterval)
            {
                spawner.Timer -= spawner.SpawnInterval;

                var random = Unity.Mathematics.Random.CreateFromIndex(Seed ^ (uint)sortKey);

                for (int i = 0; i < spawner.SpawnAmount; i++)
                {
                    var randomPrefabIndex = random.NextInt(0, prefabs.Length);
                    var selectedType = prefabs[randomPrefabIndex].ModelType;

                    Ecb.AppendToBuffer(sortKey, entity, new MinionSpawnRequest
                    {
                        ModelType = selectedType,
                        Position = worldTransform.ValueRO.Position
                    });
                }
            }
        }
    }
}