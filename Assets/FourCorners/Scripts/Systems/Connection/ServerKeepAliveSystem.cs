using FourCorners.Scripts.Components.Connection;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Consumes the KeepAliveRpc from clients to prevent them from piling up
    /// and ensures the server registers the UDP traffic, keeping the Relay connection alive.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerKeepAliveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            var query = SystemAPI.QueryBuilder().WithAll<KeepAliveRpc, ReceiveRpcCommandRequest>().Build();
            state.RequireForUpdate(query);
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (req, entity) in SystemAPI.Query<RefRO<KeepAliveRpc>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
