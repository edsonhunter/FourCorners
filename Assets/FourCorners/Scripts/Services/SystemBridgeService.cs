using System;
using ElementLogicFail.Scripts.Components.Bounds;
using ElementLogicFail.Scripts.Components.Request;
using ElementLogicFail.Scripts.Services.Interface;
using FourCorners.Scripts.Systems.Camera;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace ElementLogicFail.Scripts.Services
{
    public class SystemBridgeService : ISystemBridgeService
    {
        public Action<Vector3> OnCameraFocus { get; set; }

        public (Vector3 min, Vector3 max) GetMapBounds()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return (Vector3.zero, Vector3.zero);
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(WanderArea));

            if (!query.IsEmpty)
            {
                var area = query.GetSingleton<WanderArea>();
                return (area.MinArea, area.MaxArea);
            }
            
            return (Vector3.zero, Vector3.zero);
        }
    }
}
