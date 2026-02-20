using ElementLogicFail.Scripts.Controller;
using ElementLogicFail.Scripts.Scenes.Interface;
using ElementLogicFail.Scripts.Services.Interface;
using UnityEngine;

namespace ElementLogicFail.Scripts.Scenes
{
    public class GameplaySceneController : BaseScene<GameplayData>
    {
        [SerializeField] private CameraController _cameraController;

        protected override void Loaded()
        {
            base.Loaded();
            
            var service = GetService<ISystemBridgeService>();
            var bounds = service.GetMapBounds();
            
            _cameraController.Init(bounds.min, bounds.max);
        }
    }
    
    public class GameplayData : ISceneData { }
}