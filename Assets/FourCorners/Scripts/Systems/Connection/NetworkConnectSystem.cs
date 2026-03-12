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
        private bool _requestSent;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();
            
            _connectionQuery = state.GetEntityQuery(builder);
            _sceneQuery = state.GetEntityQuery(ComponentType.ReadOnly<SceneReference>());
            _waitTimer = 0;
            _requestSent = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_connectionQuery.IsEmptyIgnoreFilter)
            {
                // Either not connected, or already marked as InGame by server
                return;
            }

            _waitTimer += SystemAPI.Time.DeltaTime;

            // Step 1: Send the request as soon as we have a connection
            if (!_requestSent)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                using var connectionEntities = _connectionQuery.ToEntityArray(Allocator.Temp);
                foreach (var entity in connectionEntities)
                {
                    UnityEngine.Debug.Log($"[ClientRequestGameSystem] Sending GoInGameRequest for connection {entity}");
                    var req = ecb.CreateEntity();
                    ecb.AddComponent<GoInGameRequest>(req);
                    ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
                }
                ecb.Playback(state.EntityManager);
                _requestSent = true;
            }

            // Step 2: Normally we'd wait for the server to add NetworkStreamInGame.
            // However, we still check scene loading here to log if we are stalled.
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

            if (!allScenesLoaded && UnityEngine.Time.frameCount % 120 == 0)
            {
                UnityEngine.Debug.Log($"[ClientRequestGameSystem] Pending scene loading...");
            }
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
