using FourCorners.Scripts.Components.Connection;
using FourCorners.Scripts.Components.Request;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Client-side system that waits until the GameplayScene is fully loaded
    /// and its ECS subscenes are baked. This is indicated by SceneSystem.IsSceneLoaded
    /// and the presence of SceneLoadedTag (added by GameplaySceneController AFTER map bounds check).
    ///
    /// Once ready, it brings the client connection "in game" locally and sends
    /// ReadyForGhostsRequest to the server so Ghost streaming begins safely.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientStreamReadySystem : ISystem
    {
        private EntityQuery _sceneQuery;
        private EntityQuery _pendingNetworkIdQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SceneLoadedTag>();

            _sceneQuery = state.GetEntityQuery(ComponentType.ReadOnly<SceneReference>());

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId, ClientLobbyJoinedTag>()
                .WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_pendingNetworkIdQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Verify all Netcode subscenes referenced in the new world state are fully loaded and baked
            using var sceneEntities = _sceneQuery.ToEntityArray(Allocator.Temp);

            // [FIX] Prevent premature handshake. If length is 0, the SubScene hasn't 
            // started pushing entities into the world yet.
            if (sceneEntities.Length == 0) return;

            foreach (var sceneEntity in sceneEntities)
            {
                if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, sceneEntity)) return;
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            using var connectionEntities = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in connectionEntities)
            {
                UnityEngine.Debug.Log("[ClientStreamReadySystem] SubScenes baked! Adding local NetworkStreamInGame and sending ReadyForGhostsRequest.");

                // Start expecting ghost snapshots locally
                ecb.AddComponent<NetworkStreamInGame>(entity);

                // Notify server to start transmitting ghosts
                var req = ecb.CreateEntity();
                ecb.AddComponent<ReadyForGhostsRequest>(req);
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }
        }
    }
}
