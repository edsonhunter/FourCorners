using ElementLogicFail.Scripts.Manager.Interface;
using ElementLogicFail.Scripts.MonoBridge;
using ElementLogicFail.Scripts.Scenes.Interface;
using ElementLogicFail.Scripts.Services.Interface;
using UnityEngine;
using UnityEngine.UI;

namespace ElementLogicFail.Scripts.Scenes
{
    public class ConfigScene : BaseScene<ConfigData>
    {
        [field: SerializeField] private Button backButton;
        [field: SerializeField] private MultiplayerTestUI multiplayerTestUI;

        protected override void Loaded()
        {
            backButton.onClick.AddListener(Back);
            var multiplayerService = GetService<IMultiplayerService>();
            multiplayerTestUI.Init(multiplayerService.AuthenticateAsync(), multiplayerService.HostRelayGameAsync, multiplayerService.JoinRelayGameAsync);
        }

        private void Back()
        {
            GetManager<ISceneManager>().UnloadOverlay(this);       
        }
    }

    public class ConfigData : ISceneData
    {
    }
}