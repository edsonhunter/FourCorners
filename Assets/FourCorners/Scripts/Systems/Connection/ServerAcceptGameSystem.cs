using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Components.Spawner;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Connection
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerAcceptGameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<GoInGameRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, receive, rpcEntity) in
                     SystemAPI.Query<RefRO<GoInGameRequest>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
            {
                var sourceConnection = receive.ValueRO.SourceConnection;

                if (state.EntityManager.Exists(sourceConnection))
                {
                    ecb.DestroyEntity(rpcEntity);
                    ecb.AddComponent<NetworkStreamInGame>(sourceConnection);
                    ecb.AddComponent<PendingBaseAllocation>(sourceConnection);
                }
                else
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ServerAcceptGameSystem] Received GoInGameRequest but connection {sourceConnection} does not exist!");
                }

                var gameStartRpc = ecb.CreateEntity();
                ecb.AddComponent<SendRpcCommandRequest>(gameStartRpc);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
