using FourCorners.Scripts.Components.Request;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace FourCorners.Scripts.Systems.Spawner
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SpawnerSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerSpawnRpcSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ReceiveRpcCommandRequest, SpawnMinionRpc>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Pre-build a map of NetworkId -> Spawner entity to avoid O(N*M) iteration
            var spawnerMap = new NativeParallelHashMap<int, Entity>(8, Allocator.Temp);
            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<Components.Spawner.SpawnerData>>()
                         .WithEntityAccess())
            {
                if (spawner.ValueRO.IsActive)
                {
                    spawnerMap.TryAdd(spawner.ValueRO.NetworkId, entity);
                }
            }

            foreach (var (reqSrc, reqRpc, reqEntity) in SystemAPI.Query<ReceiveRpcCommandRequest, SpawnMinionRpc>()
                         .WithEntityAccess())
            {
                if (SystemAPI.HasComponent<NetworkId>(reqSrc.SourceConnection))
                {
                    var networkId = SystemAPI.GetComponent<NetworkId>(reqSrc.SourceConnection);

                    if (spawnerMap.TryGetValue(networkId.Value, out var spawnerEntity))
                    {
                        var spawner = SystemAPI.GetComponentRW<Components.Spawner.SpawnerData>(spawnerEntity);

                        if (spawner.ValueRO.SpawnAmount > 0)
                        {
                            var position = SystemAPI.GetComponent<LocalTransform>(spawnerEntity).Position;

                            for (int i = 0; i < spawner.ValueRO.SpawnAmount; i++)
                            {
                                ecb.AppendToBuffer(spawnerEntity, new MinionSpawnRequest
                                {
                                    ModelType = reqRpc.ModelType,
                                    Position = position
                                });
                            }
                        }
                    }
                }

                ecb.DestroyEntity(reqEntity);
            }

            spawnerMap.Dispose();
        }
    }
}