using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Services.Interface;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Client-side system that listens for LobbyStateUpdateRpc from the server.
    ///
    /// On receipt, fires ISystemBridgeService.OnLobbyStateUpdate to drive the lobby UI:
    ///   - Updates the player count label.
    ///   - Shows the Start button only for the host when PlayerCount >= 2.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ClientLobbyStateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var rpcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LobbyStateUpdateRpc, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(rpcQuery));
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var bridgeSystem = state.World.GetExistingSystemManaged<BridgeServiceAccessSystem>();

            foreach (var (rpc, reqEntity) in
                     SystemAPI.Query<RefRO<LobbyStateUpdateRpc>>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                UnityEngine.Debug.Log(
                    $"[ClientLobbyStateSystem] Lobby update: PlayerCount={rpc.ValueRO.PlayerCount}, IsHost={rpc.ValueRO.IsHost}.");

                bridgeSystem?.BridgeService?.OnLobbyStateUpdate?.Invoke(new LobbyStateUpdateEvent
                {
                    IsHost = rpc.ValueRO.IsHost,
                    PlayerCount = rpc.ValueRO.PlayerCount
                });

                ecb.DestroyEntity(reqEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Thin managed SystemBase that holds a reference to ISystemBridgeService.
    /// Allows unmanaged ISystem structs to call into the managed service layer.
    ///
    /// Inject via BridgeServiceAccessSystem.SetBridgeService(service) in your Bootstrapper.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class BridgeServiceAccessSystem : SystemBase, ISystemBridgeServiceAccessor
    {
        public ISystemBridgeService BridgeService { get; private set; }

        protected override void OnCreate()
        {
        }

        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// Called from the Bootstrapper after DI registration to inject the service.
        /// Example in Bootstrapper.RegisterServices:
        ///   foreach (var world in World.All)
        ///       world.GetExistingSystemManaged&lt;BridgeServiceAccessSystem&gt;()?.SetBridgeService(bridge);
        /// </summary>
        public void SetBridgeService(ISystemBridgeService service) => BridgeService = service;

        /// <summary>Called by ClientMatchStartedSystem to fire the bridge event.</summary>
        public void FireMatchStarted() => BridgeService?.OnMatchStarted?.Invoke();
    }
}