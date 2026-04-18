using UnityEngine;

namespace FourCorners.Scripts.Controller
{
    public interface ICameraInputHandler
    {
        Vector2 GetMoveInput();
        float GetZoomInput();
        void EnableControls();
        void DisableControls();
    }
}
