using System.Threading.Tasks;
using ElementLogicFail.Scripts.Controller;
using ElementLogicFail.Scripts.Manager.Interface;
using ElementLogicFail.Scripts.Scenes.Interface;
using ElementLogicFail.Scripts.Services.Interface;
using UnityEngine;

namespace ElementLogicFail.Scripts.Scenes
{
    public class GameplaySceneController : BaseScene<GameplayData>
    {
        [SerializeField] private CameraController cameraController;
        private (Vector3 min, Vector3 max) _bounds;

        protected override Task Loading()
        {
            return WaitAndInitCameraAsync();
        }

        protected override void Loaded()
        {
            if (cameraController != null)
            {
                var cameraManager = GetManager<ICameraManager>();
                cameraController.Init(cameraManager, _bounds.min, _bounds.max);
            }
        }

        private async Task WaitAndInitCameraAsync()
        {
            var service = GetService<ISystemBridgeService>();
            _bounds = service.GetMapBounds();
            
            // Wait until the ECS EntityManager has created the WanderArea singleton
            while(_bounds.min == Vector3.zero && _bounds.max == Vector3.zero)
            {
                await Task.Yield(); 
                _bounds = service.GetMapBounds();
            }
            
            cameraController.Setup();
        }
    }
    
    public class GameplayData : ISceneData { }
}