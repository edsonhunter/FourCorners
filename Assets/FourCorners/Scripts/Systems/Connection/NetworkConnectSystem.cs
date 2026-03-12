using System.Linq;
using ElementLogicFail.Scripts.Components.Spawner;
using ElementLogicFail.Scripts.Components.Request;
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
        private float _waitTimer;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();
            _connectionQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_connectionQuery);

            _sceneQuery = state.GetEntityQuery(ComponentType.ReadOnly<SceneReference>());
            _waitTimer = 0;
        }

        public void OnUpdate(ref SystemState state)
        {
            _waitTimer += SystemAPI.Time.DeltaTime;

            // Wait for all registered scenes and subscenes to be fully loaded and resolved.
            bool allScenesLoaded = true;
            int sceneCount = 0;

            using var sceneEntities = _sceneQuery.ToEntityArray(Allocator.Temp);
            sceneCount = sceneEntities.Length;

            foreach (var sceneEntity in sceneEntities)
            {
                if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, sceneEntity))
                {
                    allScenesLoaded = false;
                    break;
                }
            }

            // If we have no scenes yet, we wait up to 2 seconds just in case subscenes 
            // are being loaded asynchronously as entities. Afterward, we proceed.
            if (sceneCount == 0 && _waitTimer < 2.0f)
            {
                return;
            }
            
            if (!allScenesLoaded)
            {
                if (UnityEngine.Time.frameCount % 60 == 0)
                {
                    UnityEngine.Debug.Log($"[ClientRequestGameSystem] Waiting for {sceneCount} scenes to load...");
                }
                return;
            }

            UnityEngine.Debug.Log($"[ClientRequestGameSystem] All {sceneCount} scenes loaded (or skipped after timeout). Sending GoInGameRequest.");

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

    // Server system: receives GoInGameRequest, immediately adds NetworkStreamInGame to
    // the connection entity (which triggers Netcode to activate prespawned ghosts),
    // and tags the connection with PendingBaseAllocation for the next frame.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerAcceptGameSystem : ISystem
    {
        private EntityQuery _rpcQuery;
        private EntityQuery _allRequestsQuery;

        public void OnCreate(ref SystemState state)
        {
            _rpcQuery = state.GetEntityQuery(ComponentType.ReadOnly<GoInGameRequest>(), ComponentType.ReadOnly<ReceiveRpcCommandRequest>());
            _allRequestsQuery = state.GetEntityQuery(ComponentType.ReadOnly<GoInGameRequest>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_allRequestsQuery.IsEmptyIgnoreFilter)
            {
                if (UnityEngine.Time.frameCount % 30 == 0)
                {
                    UnityEngine.Debug.Log($"[ServerAcceptGameSystem] Found {_allRequestsQuery.CalculateEntityCount()} GoInGameRequest entities.");
                }
            }

            if (_rpcQuery.IsEmptyIgnoreFilter)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (req, receive, rpcEntity) in
                SystemAPI.Query<RefRO<GoInGameRequest>, RefRO<ReceiveRpcCommandRequest>>()
                    .WithEntityAccess())
            {
                var sourceConnection = receive.ValueRO.SourceConnection;

                if (state.EntityManager.Exists(sourceConnection))
                {
                    UnityEngine.Debug.Log($"[ServerAcceptGameSystem] Accepting GoInGameRequest for connection {sourceConnection}");

                    // Step 1: Add NetworkStreamInGame. Netcode will now activate prespawned
                    // ghosts for this connection (removing their Disabled tag) on next frame.
                    ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

                    // Step 2: Tag the connection so BaseAllocationSystem assigns a base
                    // on the NEXT frame, after prespawned ghosts are fully activated.
                    ecb.AddComponent<PendingBaseAllocation>(sourceConnection);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[ServerAcceptGameSystem] Received GoInGameRequest but connection {sourceConnection} does not exist!");
                }

                ecb.DestroyEntity(rpcEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
