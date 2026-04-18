using FourCorners.Scripts.Components.Connection;
using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Components.Spawner;
using FourCorners.Scripts.Components.Team;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Server-side system that handles the GoInGameRequest RPC. Validation flow:
    ///   1. Look up the MatchStateTag entity's DynamicBuffer[TeamStatusElement].
    ///   2. Try to grant the client's desired team; fallback to any free slot.
    ///   3. If no slot exists, send TeamRejectedRpc and drop the request.
    ///   4. On success:
    ///      a. Mark the team slot occupied.
    ///      b. Add the player to ConnectedPlayerElement buffer.
    ///      c. If this is the FIRST player, assign HostTag to their connection entity.
    ///      d. Transition MatchState: WaitingForPlayers → Lobby once first player arrives.
    ///      e. Broadcast LobbyStateUpdateRpc to ALL connected players.
    ///      f. Add NetworkStreamInGame + PendingBaseAllocation.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerAcceptGameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchStateTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            var rpcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(rpcQuery));

            var matchQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MatchStateTag, TeamStatusElement, MatchState>();
            state.RequireForUpdate(state.GetEntityQuery(matchQuery));
        }

        public void OnUpdate(ref SystemState state)
        {
            var teamBuffer      = SystemAPI.GetSingletonBuffer<TeamStatusElement>(isReadOnly: false);
            var playerBuffer    = SystemAPI.GetSingletonBuffer<ConnectedPlayerElement>(isReadOnly: false);
            var matchStateEntity = SystemAPI.GetSingletonEntity<MatchStateTag>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, receive, rpcEntity) in
                     SystemAPI.Query<RefRO<GoInGameRequest>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                var sourceConnection = receive.ValueRO.SourceConnection;

                if (!state.EntityManager.Exists(sourceConnection))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ServerAcceptGameSystem] Connection {sourceConnection} no longer exists. Dropping request.");
                    ecb.DestroyEntity(rpcEntity);
                    continue;
                }

                // --- Team Availability Resolution ---
                int desired     = request.ValueRO.RequestedTeamIndex;
                int grantedTeam = -1;

                if (desired >= 0 && desired < teamBuffer.Length && !teamBuffer[desired].IsOccupied)
                {
                    grantedTeam = desired;
                }
                else
                {
                    for (int i = 0; i < teamBuffer.Length; i++)
                    {
                        if (!teamBuffer[i].IsOccupied) { grantedTeam = i; break; }
                    }
                }

                ecb.DestroyEntity(rpcEntity);

                if (grantedTeam == -1)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ServerAcceptGameSystem] All 4 teams occupied. Rejecting connection {sourceConnection}.");
                    var rejectionRpc = ecb.CreateEntity();
                    ecb.AddComponent<TeamRejectedRpc>(rejectionRpc);
                    ecb.AddComponent(rejectionRpc, new SendRpcCommandRequest { TargetConnection = sourceConnection });
                    continue;
                }

                // --- Grant the team slot ---
                teamBuffer[grantedTeam] = new TeamStatusElement
                {
                    IsOccupied      = true,
                    OccupyingPlayer = sourceConnection
                };

                var networkId = SystemAPI.GetComponent<NetworkId>(sourceConnection);

                // --- First player ever? Assign HostTag ---
                bool isHost = playerBuffer.IsEmpty;
                if (isHost)
                {
                    ecb.AddComponent<HostTag>(sourceConnection);
                    UnityEngine.Debug.Log($"[ServerAcceptGameSystem] HostTag assigned to NetworkId={networkId.Value}.");

                    // WaitingForPlayers → Lobby: at least one player has arrived
                    ecb.SetComponent(matchStateEntity, new MatchState { Phase = MatchPhase.Lobby });
                }

                // Register player in the connected roster
                playerBuffer.Add(new ConnectedPlayerElement
                {
                    NetworkId        = networkId.Value,
                    ConnectionEntity = sourceConnection
                });

                int newPlayerCount = playerBuffer.Length;

                UnityEngine.Debug.Log(
                    $"[ServerAcceptGameSystem] Granted Team {grantedTeam} to NetworkId={networkId.Value} " +
                    $"(isHost={isHost}). Total players: {newPlayerCount}.");

                // --- Broadcast LobbyStateUpdateRpc to every accepted player ---
                // We iterate the player buffer (which now includes the new player).
                for (int i = 0; i < playerBuffer.Length; i++)
                {
                    var targetConn   = playerBuffer[i].ConnectionEntity;
                    // Fix: We can't use HasComponent<HostTag> immediately because it was just added via ECB.
                    // The first player in the buffer (index 0) is always the host.
                    bool targetIsHost = (i == 0);

                    var lobbyRpc = ecb.CreateEntity();
                    ecb.AddComponent(lobbyRpc, new LobbyStateUpdateRpc
                    {
                        IsHost      = targetIsHost,
                        PlayerCount = newPlayerCount
                    });
                    ecb.AddComponent(lobbyRpc, new SendRpcCommandRequest { TargetConnection = targetConn });
                }

                // --- Transition to Active is handled when HostStartGameSystem fires ---
                // Note: We DO NOT add NetworkStreamInGame here. Ghost synchronization
                // is deferred. The client will send ReadyForGhostsRequest once it transitions
                // to GameplayScene and finishes baking its subscenes.
            }
        }
    }
}