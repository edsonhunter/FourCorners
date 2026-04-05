using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Components.Spawner;
using FourCorners.Scripts.Components.Team;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Server-side system that handles the GoInGameRequest RPC, which now carries a
    /// RequestedTeamIndex (0-3). Validation flow:
    ///   1. Look up the MatchStateTag entity's DynamicBuffer[TeamStatusElement].
    ///   2. Try to grant the client's desired team.
    ///   3. If taken, scan for any free slot (fallback).
    ///   4. If no slot exists, send TeamRejectedRpc back to the client and drop the request.
    ///   5. On success, mark the slot occupied, add NetworkStreamInGame, and add
    ///      PendingBaseAllocation (with the approved team) to the connection entity.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerAcceptGameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            // Only run when there are pending join RPCs
            var rpcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(rpcQuery));

            // Also require the MatchState entity to exist so the buffer is accessible
            var matchQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MatchStateTag, TeamStatusElement>();
            state.RequireForUpdate(state.GetEntityQuery(matchQuery));
        }

        public void OnUpdate(ref SystemState state)
        {
            // GetSingletonBuffer gives direct read/write access to the 4-element buffer.
            // isReadOnly: false because we mark slots as occupied in-place.
            var teamBuffer = SystemAPI.GetSingletonBuffer<TeamStatusElement>(isReadOnly: false);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, receive, rpcEntity) in
                     SystemAPI.Query<RefRO<GoInGameRequest>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                var sourceConnection = receive.ValueRO.SourceConnection;

                // Guard: connection must still exist (client may have dropped mid-frame)
                if (!state.EntityManager.Exists(sourceConnection))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ServerAcceptGameSystem] Connection {sourceConnection} no longer exists. Dropping request.");
                    ecb.DestroyEntity(rpcEntity);
                    continue;
                }

                int desired = request.ValueRO.RequestedTeamIndex;

                // --- Team Availability Resolution ---
                int grantedTeam = -1;

                // 1. Try the desired team first
                if (desired >= 0 && desired < teamBuffer.Length && !teamBuffer[desired].IsOccupied)
                {
                    grantedTeam = desired;
                }
                else
                {
                    // 2. Fallback: scan all 4 slots for any free one
                    for (int i = 0; i < teamBuffer.Length; i++)
                    {
                        if (!teamBuffer[i].IsOccupied)
                        {
                            grantedTeam = i;
                            break;
                        }
                    }
                }

                ecb.DestroyEntity(rpcEntity);

                if (grantedTeam == -1)
                {
                    // No slot available — reject the client
                    UnityEngine.Debug.LogWarning(
                        $"[ServerAcceptGameSystem] All 4 teams are occupied. Sending TeamRejectedRpc to {sourceConnection}.");

                    var rejectionRpc = ecb.CreateEntity();
                    ecb.AddComponent<TeamRejectedRpc>(rejectionRpc);
                    ecb.AddComponent(rejectionRpc, new SendRpcCommandRequest
                    {
                        TargetConnection = sourceConnection
                    });
                    continue;
                }

                // --- Grant the team slot ---
                // Write back directly into the singleton buffer (no structural change needed)
                teamBuffer[grantedTeam] = new TeamStatusElement
                {
                    IsOccupied = true,
                    OccupyingPlayer = sourceConnection
                };

                UnityEngine.Debug.Log(
                    $"[ServerAcceptGameSystem] Granted Team {grantedTeam} to connection {sourceConnection} " +
                    $"(requested {desired}).");

                // Bring the connection into the game simulation
                ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

                // Hand off to BaseAllocationSystem with the exact approved team
                ecb.AddComponent(sourceConnection, new PendingBaseAllocation
                {
                    ApprovedTeam = (TeamNumber)grantedTeam
                });

                // Notify all clients that the game has started / a player joined
                var gameStartRpc = ecb.CreateEntity();
                ecb.AddComponent<SendRpcCommandRequest>(gameStartRpc);
            }
        }
    }
}