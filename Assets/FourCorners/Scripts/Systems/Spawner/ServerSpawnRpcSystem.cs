using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Request;
using ElementLogicFail.Scripts.Components.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Spawner
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
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ReceiveRpcCommandRequest, SpawnMinionRpc>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Pre-build a map of NetworkId -> Spawner entity to avoid O(N*M) iteration
            var spawnerMap = new NativeParallelHashMap<int, Entity>(8, Allocator.Temp);
            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<Components.Spawner.Spawner>>().WithEntityAccess())
            {
                if (spawner.ValueRO.IsActive)
                {
                    spawnerMap.TryAdd(spawner.ValueRO.NetworkId, entity);
                }
            }

            foreach (var (reqSrc, reqRpc, reqEntity) in SystemAPI.Query<ReceiveRpcCommandRequest, SpawnMinionRpc>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<NetworkId>(reqSrc.SourceConnection))
                {
                    var networkId = SystemAPI.GetComponent<NetworkId>(reqSrc.SourceConnection);

                    if (spawnerMap.TryGetValue(networkId.Value, out var spawnerEntity))
                    {
                        var spawner = SystemAPI.GetComponentRW<Components.Spawner.Spawner>(spawnerEntity);
                        
                        // Removed the spawner.Timer check to decouple manual RPC spawns from the automatic wave timer.
                        if (spawner.ValueRO.SpawnAmount > 0)
                        {
                            var position = SystemAPI.GetComponent<LocalTransform>(spawnerEntity).Position;

                            for (int i = 0; i < spawner.ValueRO.SpawnAmount; i++)
                            {
                                ecb.AppendToBuffer(spawnerEntity, new ElementSpawnRequest
                                {
                                    Type = spawner.ValueRO.Team,
                                    ModelType = reqRpc.ModelType,
                                    Position = position
                                });
                            }
                        }
                    }
                }

                ecb.DestroyEntity(reqEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            spawnerMap.Dispose();
        }
    }
}
