using System;
using UnityEngine;

namespace FourCorners.Scripts.Services.Interface
{
    public interface ISystemBridgeService : IService
    {
        (Vector3 min, Vector3 max) GetMapBounds();
        void NotifyClientSceneReady();
    }
}
