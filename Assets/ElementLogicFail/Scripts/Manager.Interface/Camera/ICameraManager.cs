using ElementLogicFail.Scripts.Controller;
using UnityEngine;

namespace ElementLogicFail.Scripts.Manager.Interface.Camera
{
    public interface ICameraManager : IManager
    {
        ICameraInputHandler InputHandler { get; }
        ICameraBoundsCalculator BoundsCalculator { get; }
        void Initialize(Vector3 mapMin, Vector3 mapMax);
    }
}
