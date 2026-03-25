using System;
using ElementLogicFail.Scripts.Components.Bounds;
using ElementLogicFail.Scripts.Components.Spawner;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace FourCorners.Scripts.Systems.Camera
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class LocalPlayerCameraSystem : SystemBase
    {
        public Action<float3> OnCameraFocus;

        protected override void OnCreate()
        {
            RequireForUpdate<NetworkStreamInGame>();
        }
        
        protected override void OnUpdate()
        {
            var connectionQuery = SystemAPI.QueryBuilder().WithAll<NetworkId, NetworkStreamInGame>().Build();
            if (connectionQuery.IsEmpty) return;
            
            var connectionEntity = connectionQuery.GetSingletonEntity();
            int localNetworkId = SystemAPI.GetComponent<NetworkId>(connectionEntity).Value;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (transform, baseData, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerBase>>()
                         .WithNone<CameraFocusInitializedTag>()
                         .WithEntityAccess())
            {
                if (baseData.ValueRO.NetworkId == localNetworkId)
                {
                    OnCameraFocus?.Invoke(transform.ValueRO.Position);
                    ecb.AddComponent<CameraFocusInitializedTag>(entity);
                }
            }
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}