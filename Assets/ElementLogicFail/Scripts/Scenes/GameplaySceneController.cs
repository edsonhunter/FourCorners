using System.Threading.Tasks;
using ElementLogicFail.Scripts.Controller;
using ElementLogicFail.Scripts.Scenes.Interface;
using ElementLogicFail.Scripts.Services.Interface;
using UnityEngine;

namespace ElementLogicFail.Scripts.Scenes
{
    public class GameplaySceneController : BaseScene<GameplayData>
    {
        [SerializeField] private CameraController _cameraController;

        protected override Task Loading()
        {
            return WaitAndInitCameraAsync();
        }

        private async Task WaitAndInitCameraAsync()
        {
            var service = GetService<ISystemBridgeService>();
            var bounds = service.GetMapBounds();
            
            // Wait until the ECS EntityManager has created the WanderArea singleton
            while(bounds.min == Vector3.zero && bounds.max == Vector3.zero)
            {
                await Task.Yield(); 
                bounds = service.GetMapBounds();
            }
            
            if (_cameraController != null)
            {
                _cameraController.Init(bounds.min, bounds.max);
            }
        }
    }
    
    public class GameplayData : ISceneData { }
}