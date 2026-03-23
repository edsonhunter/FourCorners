using ElementLogicFail.Scripts.Components.Spawner;
using ElementLogicFail.Scripts.Components.Request;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Systems.Connection
{
    // Client system: fires once when our NetworkId appears. Marks client as InGame
    // locally and sends GoInGameRequest RPC to server.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameSystem : ISystem
    {
        private EntityQuery _pendingNetworkIdQuery;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_pendingNetworkIdQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            using var connectionEntities = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in connectionEntities)
            {
                ecb.AddComponent<NetworkStreamInGame>(entity);
                UnityEngine.Debug.Log($"[ClientRequestGameSystem] Sending GoInGameRequest for connection {entity}");
                var req = ecb.CreateEntity();
                ecb.AddComponent<GoInGameRequest>(req);
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }

            ecb.Playback(state.EntityManager);
        }
    }
}

// Server system: receives GoInGameRequest, immediately adds NetworkStreamInGame to
// the connection entity (which triggers Netcode to activate prespawned ghosts),
// and tags the connection with PendingBaseAllocation for the next frame.
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
                 SystemAPI.Query<GoInGameRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            var sourceConnection = receive.SourceConnection;

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