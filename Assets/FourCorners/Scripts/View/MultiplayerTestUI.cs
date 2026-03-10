using ElementLogicFail.Scripts.Manager;
using ElementLogicFail.Scripts.Services.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementLogicFail.Scripts.MonoBridge
{
    public class MultiplayerTestUI : MonoBehaviour
    {
        public TMP_InputField JoinCodeInput;
        public TextMeshProUGUI StatusText;
        public Button HostButton;
        public Button JoinButton;

        private IMultiplayerService _multiplayerService;

        private async void Start()
        {
            var appManager = FindFirstObjectByType<ApplicationManager>();
            if (appManager == null)
            {
                Debug.LogError("No ApplicationManager found!");
                return;
            }

            _multiplayerService = appManager.GetService<IMultiplayerService>();
            
            StatusText.text = "Authenticating...";
            await _multiplayerService.AuthenticateAsync();
            StatusText.text = "Authenticated. Ready to Host or Join.";
            
            HostButton.onClick.AddListener(HostGame);
            JoinButton.onClick.AddListener(JoinGame);
        }

        public async void HostGame()
        {
            StatusText.text = "Hosting...";
            string code = await _multiplayerService.HostGameAsync(4);
            StatusText.text = $"Hosted! Join Code: {code}";
            Debug.Log($"Join Code: {code}");
            GUIUtility.systemCopyBuffer = code;
        }

        public async void JoinGame()
        {
            if (string.IsNullOrEmpty(JoinCodeInput.text))
            {
                StatusText.text = "Enter a Join Code first!";
                return;
            }

            StatusText.text = "Joining...";
            await _multiplayerService.JoinGameAsync(JoinCodeInput.text);
            StatusText.text = "Joined!";
        }
    }
}
