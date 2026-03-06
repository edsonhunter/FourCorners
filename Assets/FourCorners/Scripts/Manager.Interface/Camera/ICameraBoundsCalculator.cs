using UnityEngine;

namespace ElementLogicFail.Scripts.Controller
{
    public interface ICameraBoundsCalculator
    {
        void Initialize(Vector3 mapMin, Vector3 mapMax);
        void CalculateDynamicBounds(Camera camera, Transform cameraTransform, float mapPadding);
        Vector2 MapLimitX { get; }
        Vector2 MapLimitZ { get; }
    }
}
