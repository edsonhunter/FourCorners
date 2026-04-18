using FourCorners.Scripts.Controller;
using UnityEngine;

namespace FourCorners.Scripts.Manager.Interface.Camera
{
    public interface ICameraManager : IManager
    {
        ICameraInputHandler InputHandler { get; }
        ICameraBoundsCalculator BoundsCalculator { get; }
        void Initialize(Vector3 mapMin, Vector3 mapMax);
    }
}
