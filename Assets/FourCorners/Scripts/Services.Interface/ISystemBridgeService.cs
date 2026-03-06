using ElementLogicFail.Scripts.Services.Interface;
using UnityEngine;

namespace ElementLogicFail.Scripts.Services.Interface
{
    public interface ISystemBridgeService : IService
    {
        (Vector3 min, Vector3 max) GetMapBounds();
    }
}
