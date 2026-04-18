using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FourCorners.Scripts.Services.Interface
{
    public interface ISystemBridgeService : IService
    {
        (Vector3 min, Vector3 max) GetMapBounds();
        void NotifyClientSceneReady();

        /// <summary>
        /// Fired by ClientLobbyStateSystem when a LobbyStateUpdateRpc arrives.
        /// UI subscribes to update player count and show/hide the Start button.
        /// </summary>
        Action<LobbyStateUpdateEvent> OnLobbyStateUpdate { get; set; }

        /// <summary>
        /// Fired by ClientMatchStartedSystem when the server broadcasts MatchStartedRpc.
        /// Every client in the lobby subscribes to this to transition → GameplayScene.
        /// </summary>
        Action OnMatchStarted { get; set; }

        /// <summary>
        /// Called by the Host's Start button. Creates a StartGameRequest RPC entity
        /// inside the active ECS Client world and sends it to the server.
        /// </summary>
        void SendStartGameRequest();

        Task RegisterBridge();
    }
}

