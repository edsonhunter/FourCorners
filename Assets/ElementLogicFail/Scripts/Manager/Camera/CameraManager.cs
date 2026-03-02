using ElementLogicFail.Scripts.Controller;
using ElementLogicFail.Scripts.Manager.Interface.Camera;
using UnityEngine;

namespace ElementLogicFail.Scripts.Manager.Camera
{
    public class CameraManager : ICameraManager
    {
        public ICameraInputHandler InputHandler { get; private set; }
        public ICameraBoundsCalculator BoundsCalculator { get; private set; }

        public CameraManager()
        {
            InputHandler = new CameraInputHandler();
            BoundsCalculator = new CameraBoundsCalculator();
        }

        public void Initialize(Vector3 mapMin, Vector3 mapMax)
        {
            BoundsCalculator.Initialize(mapMin, mapMax);
        }
    }
}
