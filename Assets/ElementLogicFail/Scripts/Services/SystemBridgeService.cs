using ElementLogicFail.Scripts.Authoring.Bounds;
using ElementLogicFail.Scripts.Services.Interface;
using UnityEngine;

namespace ElementLogicFail.Scripts.Services
{
    public class SystemBridgeService : ISystemBridgeService
    {
        public (Vector3 min, Vector3 max) GetMapBounds()
        {
            var bounds = Object.FindAnyObjectByType<BoundsAuthoring>();
            if (bounds != null)
            {
                return (bounds.min, bounds.max);
            }
            
            return (Vector3.zero, Vector3.zero);
        }
    }
}
