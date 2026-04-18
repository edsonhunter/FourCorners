using FourCorners.Scripts.Components.Connection;
using FourCorners.Scripts.Components.Request;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Client-side system that fires the GoInGameRequest RPC (with team selection)
    /// as soon as the scene is loaded and the client has a NetworkId.
    ///
    /// The desired team index is written by the UI layer via ClientRequestGameSystem.DesiredTeamIndex
    /// before the connection is established — or defaults to -1 (server auto-assigns).
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameSystem : ISystem
    {
        // Written by the UI / MonoBridge layer before the client connects.
        // -1 means "no preference — let the server assign any free slot."
        // Range: 0–3 for the four corners. Thread-safe: written once before simulation starts.
        public static int DesiredTeamIndex = -1;

        private EntityQuery _pendingNetworkIdQuery;
        private EntityQuery _sceneQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<ClientLobbyJoinedTag>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_pendingNetworkIdQuery);

            _sceneQuery = state.GetEntityQuery(ComponentType.ReadOnly<SceneReference>());
        }

        public void OnUpdate(ref SystemState state)
        {
            // Wait until all referenced sub-scenes are fully loaded
            using var sceneEntities = _sceneQuery.ToEntityArray(Allocator.Temp);
            foreach (var sceneEntity in sceneEntities)
            {
                if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, sceneEntity)) return;
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            using var connectionEntities = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in connectionEntities)
            {
                // Mark the local connection as having joined the lobby so this system won't fire again.
                // Note: We DO NOT add NetworkStreamInGame yet. Ghost streaming waits for ReadyForGhostsRequest.
                ecb.AddComponent<ClientLobbyJoinedTag>(entity);

                UnityEngine.Debug.Log(
                    $"[ClientRequestGameSystem] Sending GoInGameRequest with TeamIndex={DesiredTeamIndex}");

                var req = ecb.CreateEntity();
                ecb.AddComponent(req, new GoInGameRequest { RequestedTeamIndex = DesiredTeamIndex });
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }
        }
    }
}