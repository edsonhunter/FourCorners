using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Services;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Client-side system that listens for the server's MatchStartedRpc broadcast.
    ///
    /// When received, it fires ISystemBridgeService.OnMatchStarted, which every
    /// client in the LobbyScene (host and non-host alike) has subscribed to.
    /// The LobbySceneController's subscription then calls
    /// GetManager{ISceneManager}().LoadScene(new GameplayData()),
    /// transitioning everyone to the GameplayScene simultaneously.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ClientMatchStartedSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var rpcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MatchStartedRpc, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(rpcQuery));
        }

        public void OnUpdate(ref SystemState state)
        {
            // Use a Temp ECB here — this is safe because we're only destroying
            // RPC entities (no structural changes that affect other running jobs).
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Retrieve the bridge service via the managed accessor system.
            ISystemBridgeServiceAccessor bridgeAccessor = null;
            foreach (var world in World.All)
            {
                var accessor = world.GetExistingSystemManaged<BridgeServiceAccessSystem>();
                if (accessor != null) { bridgeAccessor = accessor; break; }
            }

            foreach (var (_, reqEntity) in
                     SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>()
                         .WithAll<MatchStartedRpc>()
                         .WithEntityAccess())
            {
                UnityEngine.Debug.Log("[ClientMatchStartedSystem] MatchStartedRpc received. Firing OnMatchStarted.");
                bridgeAccessor?.FireMatchStarted();
                ecb.DestroyEntity(reqEntity);
                break; // Only process once per frame
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>Helper interface so ClientMatchStartedSystem can call into the managed system.</summary>
    internal interface ISystemBridgeServiceAccessor
    {
        void FireMatchStarted();
    }
}
