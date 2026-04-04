using FourCorners.Scripts.Components.Request;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;

namespace FourCorners.Scripts.Systems.Connection
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameSystem : ISystem
    {
        private EntityQuery _pendingNetworkIdQuery;
        private EntityQuery _sceneQuery;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_pendingNetworkIdQuery);
            
            _sceneQuery = state.GetEntityQuery(ComponentType.ReadOnly<SceneReference>());
        }

        public void OnUpdate(ref SystemState state)
        {
            using var sceneEntities = _sceneQuery.ToEntityArray(Allocator.Temp);
            foreach (var sceneEntity in sceneEntities)
            {
                if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, sceneEntity)) return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            using var connectionEntities = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in connectionEntities)
            {
                ecb.AddComponent<NetworkStreamInGame>(entity);
                UnityEngine.Debug.Log($"[ClientRequestGameSystem] Sending GoInGameRequest for connection {entity}");
                var req = ecb.CreateEntity();
                ecb.AddComponent<GoInGameRequest>(req);
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
