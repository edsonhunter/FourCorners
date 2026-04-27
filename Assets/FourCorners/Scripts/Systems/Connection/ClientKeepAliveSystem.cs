using FourCorners.Scripts.Components.Connection;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Sends a dummy RPC every 2 seconds to keep the UDP/Relay connection alive
    /// while the players are waiting in the lobby (before NetworkStreamInGame is added).
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientKeepAliveSystem : ISystem
    {
        private float _timer;
        private EntityQuery _connectionQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();
            _connectionQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_connectionQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            _timer += SystemAPI.Time.DeltaTime;
            if (_timer < 2f) return;
            _timer = 0f;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            using var connections = _connectionQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in connections)
            {
                var req = ecb.CreateEntity();
                ecb.AddComponent<KeepAliveRpc>(req);
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }
        }
    }
}
