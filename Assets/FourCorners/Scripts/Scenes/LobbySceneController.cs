using System;
using FourCorners.Scripts.Manager.Interface;
using FourCorners.Scripts.Scenes.Interface;
using FourCorners.Scripts.Services.Interface;
using FourCorners.Scripts.View;
using UnityEngine;

namespace FourCorners.Scripts.Scenes
{
    /// <summary>
    /// Scene controller for the Lobby phase.
    ///
    /// Lifecycle:
    ///   1. Loaded() wires up LobbyScreenUI with bridge service callbacks.
    ///   2. LobbyScreenUI subscribes to ISystemBridgeService.OnLobbyStateUpdate (ECS → UI).
    ///   3. Host presses Start → bridge.SendStartGameRequest() → server validates.
    ///   4. Server broadcasts MatchStartedRpc to ALL clients.
    ///   5. ClientMatchStartedSystem fires bridge.OnMatchStarted on every client.
    ///   6. OnMatchStarted calls NavigateToGameplay() which transitions to GameplayScene.
    /// </summary>
    public class LobbySceneController : BaseScene<LobbyData>
    {
        [field: SerializeField] private LobbyScreenUI lobbyScreenUI;

        ISystemBridgeService systemBridgeService;
        event Action<int,bool> OnLobbyStateUpdate;
        protected override void Loaded()
        {
            systemBridgeService = GetService<ISystemBridgeService>();

            // Subscribe to the match-started broadcast so ALL clients transition together.
            systemBridgeService.OnMatchStarted += NavigateToGameplay;
            systemBridgeService.OnLobbyStateUpdate += LobbyUpdate;

            lobbyScreenUI.Init(
                systemBridgeService.OnMatchStarted,
                onStart: () => { /* Start is handled by ClientMatchStartedSystem via OnMatchStarted */ },
                onExit:  ExitLobby);
        }

        private void LobbyUpdate(LobbyStateUpdateEvent obj)
        {
            lobbyScreenUI.UpdateLobbyState(obj.PlayerCount, obj.IsHost);
        }

        protected override void Unload()
        {
            // Unsubscribe to prevent stale callbacks if the scene is reloaded.
            var bridge = GetService<ISystemBridgeService>();
            bridge.OnMatchStarted -= NavigateToGameplay;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Navigation
        // ──────────────────────────────────────────────────────────────────────────

        private void NavigateToGameplay()
        {
            UnityEngine.Debug.Log("[LobbySceneController] OnMatchStarted received. Transitioning to Gameplay.");
            GetManager<ISceneManager>().LoadScene(new GameplayData());
        }

        private void ExitLobby()
        {
            // The multiplayer service handles the actual disconnection from the relay/direct session.
            GetService<IMultiplayerService>().Disconnect();
            GetManager<ISceneManager>().LoadScene(new MainMenuData());
        }
    }

    public class LobbyData : ISceneData { }
}
