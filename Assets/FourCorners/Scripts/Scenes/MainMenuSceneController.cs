using ElementLogicFail.Scripts.Manager.Interface;
using ElementLogicFail.Scripts.Scenes.Interface;
using ElementLogicFail.Scripts.Services.Interface;
using ElementLogicFail.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ElementLogicFail.Scripts.Scenes
{
    public class MainMenuSceneController : BaseScene<MainMenuData>
    {
        [field: SerializeField] private Button playGameButton;
        [field: SerializeField] private Button configButton;
        [field: SerializeField] private ConnectionScreenUI connectionScreenPanel;

        protected override void Loaded()
        {
            if (connectionScreenPanel != null)
                connectionScreenPanel.gameObject.SetActive(false);

            playGameButton.onClick.AddListener(PlayGame);
            configButton.onClick.AddListener(OpenConfig);
        }

        private void PlayGame()
        {
            if (connectionScreenPanel != null)
            {
                connectionScreenPanel.gameObject.SetActive(true);
                connectionScreenPanel.Init(
                    GetService<IMultiplayerService>().AuthenticateAsync(),
                    GetService<IMultiplayerService>().HostRelayGameAsync,
                    GetService<IMultiplayerService>().JoinRelayGameAsync,
                    GetService<IMultiplayerService>().HostDirectGameAsync,
                    GetService<IMultiplayerService>().JoinDirectGameAsync,
                    StartGame);
            }
            else
            {
                // Fallback for missing UI
                GetManager<ISceneManager>().LoadScene(new GameplayData());
            }
        }

        private void OpenConfig()
        {
            GetManager<ISceneManager>().LoadOverlayScene(new ConfigData());
        }

        private void StartGame()
        {
            GetManager<ISceneManager>().LoadScene(new GameplayData());
        }
    }
    
    public class MainMenuData : ISceneData{ }
}