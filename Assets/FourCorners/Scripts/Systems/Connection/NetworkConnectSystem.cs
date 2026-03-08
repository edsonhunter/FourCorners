using ElementLogicFail.Scripts.Components.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Systems.Connection
{
    // Client system: fires once when our NetworkId appears. Marks client as InGame
    // locally and sends GoInGameRequest RPC to server.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
            {
                // Mark client-side connection as InGame immediately
                ecb.AddComponent<NetworkStreamInGame>(entity);

                // Send RPC to server requesting to enter the game
                var req = ecb.CreateEntity();
                ecb.AddComponent<GoInGameRequest>(req);
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    // RPC payload sent from client to server
    public struct GoInGameRequest : IRpcCommand { }

    // Server system: receives GoInGameRequest, immediately adds NetworkStreamInGame to
    // the connection entity (which triggers Netcode to activate prespawned ghosts),
    // and tags the connection with PendingBaseAllocation for the next frame.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerAcceptGameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            // Complete all pending Netcode jobs before touching connection entities
            state.Dependency.Complete();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (req, receive, rpcEntity) in
                SystemAPI.Query<RefRO<GoInGameRequest>, RefRO<ReceiveRpcCommandRequest>>()
                    .WithEntityAccess())
            {
                var sourceConnection = receive.ValueRO.SourceConnection;

                // Step 1: Add NetworkStreamInGame. Netcode will now activate prespawned
                // ghosts for this connection (removing their Disabled tag) on next frame.
                ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

                // Step 2: Tag the connection so BaseAllocationSystem assigns a base
                // on the NEXT frame, after prespawned ghosts are fully activated.
                ecb.AddComponent<PendingBaseAllocation>(sourceConnection);

                ecb.DestroyEntity(rpcEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
