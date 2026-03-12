using System.Linq;
using ElementLogicFail.Scripts.Components.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;

namespace ElementLogicFail.Scripts.Systems.Connection
{
    // Client system: fires once when our NetworkId appears. Marks client as InGame
    // locally and sends GoInGameRequest RPC to server.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameSystem : ISystem
    {
        private EntityQuery _connectionQuery;
        private EntityQuery _sceneQuery;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();
            _connectionQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_connectionQuery);

            _sceneQuery = state.GetEntityQuery(ComponentType.ReadOnly<SceneReference>());
        }

        public void OnUpdate(ref SystemState state)
        {
            // If the subscene hasn't even been registered in the ECS world yet
            // (due to async Unity Scene loading), we must wait here!
            if (_sceneQuery.IsEmptyIgnoreFilter)
                return;

            // Wait for all registered scenes and subscenes to be fully loaded and resolved.
            bool allScenesLoaded = true;
            using var sceneEntities = _sceneQuery.ToEntityArray(Allocator.Temp);
            
            foreach (var sceneEntity in sceneEntities)
            {
                if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, sceneEntity))
                {
                    allScenesLoaded = false;
                    break;
                }
            }
            
            if (!allScenesLoaded)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            using var connectionEntities = _connectionQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in connectionEntities)
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
