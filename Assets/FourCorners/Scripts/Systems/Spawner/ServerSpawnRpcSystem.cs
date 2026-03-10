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

            foreach (var (reqSrc, reqRpc, reqEntity) in SystemAPI.Query<ReceiveRpcCommandRequest, SpawnMinionRpc>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<NetworkId>(reqSrc.SourceConnection))
                {
                    var networkId = SystemAPI.GetComponent<NetworkId>(reqSrc.SourceConnection);

                    // Find the spawner belonging to this player
                    foreach (var (spawner, spawnerEntity) in SystemAPI.Query<RefRW<Components.Spawner.Spawner>>().WithEntityAccess())
                    {
                        if (spawner.ValueRO.IsActive && spawner.ValueRO.NetworkId == networkId.Value)
                        {
                            if (spawner.ValueRO.Timer >= spawner.ValueRO.SpawnInterval && spawner.ValueRO.SpawnAmount > 0)
                            {
                                spawner.ValueRW.Timer = 0; // Reset Cooldown
                                
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

                            break;
                        }
                    }
                }

                ecb.DestroyEntity(reqEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
