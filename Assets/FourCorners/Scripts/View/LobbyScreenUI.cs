using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FourCorners.Scripts.View
{
    /// <summary>
    /// Drives the Lobby HUD. Subscribes to ISystemBridgeService events to:
    ///   - Update the player count label when a new player joins.
    ///   - Show the Start button exclusively for the host once >= 2 players are present.
    ///
    /// Wire-up (from LobbySceneController.Init):
    ///   - startButton.onClick → fires OnStartClicked (sends RPC + scene transition).
    ///   - exitButton.onClick  → fires OnExitClicked (disconnect + loads MainMenu).
    /// </summary>
    public class LobbyScreenUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerCountLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;

        private Action OnSendStartGameRequest;
        
        private Action _onStart;
        private Action _onExit;

        // ──────────────────────────────────────────────────────────────────────────
        // Initialization
        // ──────────────────────────────────────────────────────────────────────────

        public void Init(Action onSendStartGameRequest, Action onStart, Action onExit)
        {
            OnSendStartGameRequest = onSendStartGameRequest;
            
            _onStart = onStart;
            _onExit  = onExit;

            // Default state: start hidden, count at 1 for the local player
            startButton.gameObject.SetActive(false);
            UpdatePlayerCount(1);

            startButton.onClick.AddListener(OnStartClicked);
            exitButton.onClick.AddListener(OnExitClicked);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Bridge Event Handlers
        // ──────────────────────────────────────────────────────────────────────────

        public void UpdateLobbyState(int playerCount, bool isHost)
        {
            UpdatePlayerCount(playerCount);
            startButton.gameObject.SetActive(isHost && playerCount >= 2);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Button Handlers
        // ──────────────────────────────────────────────────────────────────────────

        private void OnStartClicked()
        {
            startButton.interactable = false; // Prevent double-click
            OnSendStartGameRequest();
            // Note: the actual scene transition is driven by ClientMatchStartedSystem
            // via bridge.OnMatchStarted, not by the button directly.
            // The host will receive the MatchStartedRpc broadcast just like every client.
        }

        private void OnExitClicked() => _onExit?.Invoke();

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private void UpdatePlayerCount(int count)
        {
            if (playerCountLabel != null)
                playerCountLabel.text = $"Players: {count} / 4";
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveAllListeners();
            exitButton.onClick.RemoveAllListeners();
        }
    }
}
