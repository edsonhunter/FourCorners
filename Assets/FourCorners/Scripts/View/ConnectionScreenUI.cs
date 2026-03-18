using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementLogicFail.Scripts.UI
{
    public class ConnectionScreenUI : MonoBehaviour
    {
        [Header("Relay Connectivity")]
        public TMP_InputField joinCodeInput;
        public Button hostRelayBtn;
        public Button joinRelayBtn;

        [Header("Direct Connectivity (IP/Port)")]
        public TMP_InputField ipInput;
        public TMP_InputField portInput;
        public Button hostDirectBtn;
        public Button joinDirectBtn;

        [Header("General")]
        public Button closeBtn;
        public TextMeshProUGUI statusText;

        private Action _startGame;
        private Task _authenticateAsync;
        private Func<int, Task<string>> _hostRelayGameAsync;
        private Func<string, Task<bool>> _joinRelayGameAsync;
        private Func<ushort, Task<bool>> _hostDirectGameAsync; 
        private Func<string, ushort, Task<bool>> _joinDirectGameAsync;

        public async void Init(Task authenticateAsync, Func<int, Task<string>> hostRelayGameAsync, Func<string, Task<bool>> joinRelayGameAsync,
            Func<ushort, Task<bool>> hostDirectGameAsync, Func<string, ushort, Task<bool>> joinDirectGameAsync, Action startGame)
        {
            _startGame = startGame;
            _authenticateAsync = authenticateAsync;
            _hostRelayGameAsync = hostRelayGameAsync;
            _joinRelayGameAsync = joinRelayGameAsync;
            _hostDirectGameAsync = hostDirectGameAsync;
            _joinDirectGameAsync = joinDirectGameAsync;

            if (string.IsNullOrEmpty(ipInput.text)) ipInput.text = "127.0.0.1";
            if (string.IsNullOrEmpty(portInput.text)) portInput.text = "7777";

            hostRelayBtn.onClick.AddListener(OnHostRelayClicked);
            joinRelayBtn.onClick.AddListener(OnJoinRelayClicked);
            hostDirectBtn.onClick.AddListener(OnHostDirectClicked);
            joinDirectBtn.onClick.AddListener(OnJoinDirectClicked);
            closeBtn.onClick.AddListener(OnCloseClicked);

            SetStatus("Authenticating...");
            SetInteractable(false);
            
            await _authenticateAsync;
            
            SetInteractable(true);
            SetStatus("Ready to connect.");
        }

        private async void OnHostRelayClicked()
        {
            SetInteractable(false);
            SetStatus("Creating Relay Room...");
            
            string code = await _hostRelayGameAsync(4);
            
            if (!string.IsNullOrEmpty(code))
            {
                SetStatus($"Room Created: {code}");
                GUIUtility.systemCopyBuffer = code; // Copy to clipboard automatically
                StartGame();
            }
            else
            {
                SetStatus("Failed to create Relay Room.");
                SetInteractable(true);
            }
        }

        private async void OnJoinRelayClicked()
        {
            if (string.IsNullOrEmpty(joinCodeInput.text))
            {
                SetStatus("Please enter a valid Join Code.");
                return;
            }

            SetInteractable(false);
            SetStatus("Joining Relay Room...");

            bool success = await _joinRelayGameAsync(joinCodeInput.text);

            if (success)
            {
                SetStatus("Joined successfully!");
                StartGame();
            }
            else
            {
                SetStatus("Failed to join Relay Room. Check code.");
                SetInteractable(true);
            }
        }

        private async void OnHostDirectClicked()
        {
            if (!ushort.TryParse(portInput.text, out ushort port))
            {
                SetStatus("Invalid Port format.");
                return;
            }

            SetInteractable(false);
            SetStatus($"Hosting Direct locally on Port {port}...");

            bool success = await _hostDirectGameAsync(port);

            if (success)
            {
                SetStatus("Hosted successfully!");
                StartGame();
            }
            else
            {
                SetStatus("Failed to host Direct Game.");
                SetInteractable(true);
            }
        }

        private async void OnJoinDirectClicked()
        {
            string ip = ipInput.text;
            if (!ushort.TryParse(portInput.text, out ushort port))
            {
                SetStatus("Invalid Port format.");
                return;
            }

            SetInteractable(false);
            SetStatus($"Joining Direct Server {ip}:{port}...");

            bool success = await _joinDirectGameAsync(ip, port);

            if (success)
            {
                SetStatus("Requested Join successfully!");
                StartGame();
            }
            else
            {
                SetStatus("Failed to request Direct Join.");
                SetInteractable(true);
            }
        }

        private void OnCloseClicked()
        {
            // Just disable the window
            gameObject.SetActive(false);
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
            {
                statusText.text = msg;
            }
        }

        private void SetInteractable(bool state)
        {
            hostRelayBtn.interactable = state;
            joinRelayBtn.interactable = state;
            hostDirectBtn.interactable = state;
            joinDirectBtn.interactable = state;
            
            ipInput.interactable = state;
            portInput.interactable = state;
            joinCodeInput.interactable = state;
        }

        private void StartGame()
        {
            _startGame();
        }

        private void OnDestroy()
        {
            hostRelayBtn.onClick.RemoveAllListeners();
            joinRelayBtn.onClick.RemoveAllListeners();
            hostDirectBtn.onClick.RemoveAllListeners();
            joinDirectBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.RemoveAllListeners();
        }
    }
}
