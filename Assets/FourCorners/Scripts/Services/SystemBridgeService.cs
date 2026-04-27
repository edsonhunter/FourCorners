using System;
using System.Threading.Tasks;
using FourCorners.Scripts.Components.Bounds;
using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Services.Interface;
using FourCorners.Scripts.Systems.Camera;
using FourCorners.Scripts.Systems.Connection;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FourCorners.Scripts.Services
{
    public class SystemBridgeService : ISystemBridgeService
    {
        public Action<Vector3> OnCameraFocus { get; set; }

        /// <inheritdoc />
        public Action<LobbyStateUpdateEvent> OnLobbyStateUpdate { get; set; }

        /// <inheritdoc />
        public Action OnMatchStarted { get; set; }

        public (Vector3 min, Vector3 max) GetMapBounds()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return (Vector3.zero, Vector3.zero);

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(WanderArea));

            if (!query.IsEmpty)
            {
                var area = query.GetSingleton<WanderArea>();
                return (area.MinArea, area.MaxArea);
            }

            return (Vector3.zero, Vector3.zero);
        }

        public void NotifyClientSceneReady()
        {
            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    world.EntityManager.CreateEntity(typeof(SceneLoadedTag));
                    // Removed 'break;' so that ThinClients in the Unity Editor's Multiplayer Play Mode
                    // also receive the tag and can successfully trigger ClientStreamReadySystem.
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Creates a StartGameRequest RPC in the active Client ECS world.
        /// Called by the Host's "Start Game" button in LobbyScreenUI.
        /// The RPC is sent without a TargetConnection so Netcode broadcasts it to the Server.
        /// </remarks>
        public void SendStartGameRequest()
        {
            foreach (var world in World.All)
            {
                if (!world.IsClient()) continue;

                var em  = world.EntityManager;
                var rpc = em.CreateEntity();
                em.AddComponentData(rpc, new StartGameRequest());
                em.AddComponentData(rpc, new SendRpcCommandRequest()); // no target = server
                UnityEngine.Debug.Log("[SystemBridgeService] SendStartGameRequest sent from client world.");
                break;
            }
        }

        public Task RegisterBridge()
        {
            var tcs = new TaskCompletionSource<bool>();
            foreach (var world in World.All)
                world.GetExistingSystemManaged<BridgeServiceAccessSystem>()?.SetBridgeService(this);
            tcs.SetResult(true);
            return tcs.Task;
        }
    }
}
