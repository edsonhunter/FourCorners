using ElementLogicFail.Scripts.Components.Request;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Systems.Connection
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameSystem : ISystem
    {
        private EntityQuery _pendingNetworkIdQuery;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_pendingNetworkIdQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
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
        }
    }
}