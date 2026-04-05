using FourCorners.Scripts.Components.Spawner;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Spawner
{
    /// <summary>
    /// Assigns the exact PlayerBase and LaneSpawners to a connected player based
    /// on the ApprovedTeam stored in PendingBaseAllocation (set by ServerAcceptGameSystem).
    ///
    /// Runs only after NetworkStreamInGame is set, which triggers Netcode to activate
    /// prespawned ghost entities (remove Disabled tag). Connections waiting for a base
    /// are marked with PendingBaseAllocation carrying the approved TeamNumber.
    ///
    /// IMPORTANT: Never queries for Disabled or Prefab entities — bases must be fully
    /// active before modification to avoid prespawned ghost baseline mismatches.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BaseAllocationSystem : ISystem
    {
        private EntityQuery _baseQuery;
        private EntityQuery _spawnerQuery;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId, NetworkStreamInGame, PendingBaseAllocation>();
            state.RequireForUpdate(state.GetEntityQuery(builder));

            _baseQuery = state.GetEntityQuery(ComponentType.ReadWrite<PlayerBase>());
            state.RequireForUpdate(_baseQuery);

            _spawnerQuery = state.GetEntityQuery(ComponentType.ReadWrite<SpawnerData>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var bases = _baseQuery.ToEntityArray(Allocator.Temp);
            var spawners = _spawnerQuery.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (networkId, allocation, connectionEntity) in
                SystemAPI.Query<RefRO<NetworkId>, RefRO<PendingBaseAllocation>>()
                    .WithAll<NetworkStreamInGame>()
                    .WithEntityAccess())
            {
                int playerId = networkId.ValueRO.Value;
                var approvedTeam = allocation.ValueRO.ApprovedTeam;
                bool assigned = false;

                // Direct match: find the base whose TeamNumber equals the approved corner
                foreach (var baseEntity in bases)
                {
                    var baseData = state.EntityManager.GetComponentData<PlayerBase>(baseEntity);

                    if (baseData.TeamNumber == approvedTeam && !baseData.IsActive)
                    {
                        baseData.IsActive = true;
                        baseData.NetworkId = playerId;
                        state.EntityManager.SetComponentData(baseEntity, baseData);

                        // Activate all 3 LaneSpawners belonging to the same corner team
                        foreach (var spawnerEntity in spawners)
                        {
                            var spawnerData = state.EntityManager.GetComponentData<SpawnerData>(spawnerEntity);
                            if (spawnerData.TeamNumber == approvedTeam)
                            {
                                spawnerData.IsActive = true;
                                spawnerData.NetworkId = playerId;
                                state.EntityManager.SetComponentData(spawnerEntity, spawnerData);
                            }
                        }

                        assigned = true;
                        UnityEngine.Debug.Log(
                            $"[BaseAllocationSystem] Assigned Team={approvedTeam} Base+Spawners to NetworkId={playerId}");
                        break;
                    }
                }

                if (!assigned)
                {
                    UnityEngine.Debug.LogError(
                        $"[BaseAllocationSystem] No active base found for Team={approvedTeam} NetworkId={playerId}. " +
                        "Check that the PlayerBase in the sub-scene has the correct TeamNumber baked.");
                }

                // Remove the pending tag regardless of assignment outcome to prevent retry loops
                ecb.RemoveComponent<PendingBaseAllocation>(connectionEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            bases.Dispose();
            spawners.Dispose();
        }
    }
}
