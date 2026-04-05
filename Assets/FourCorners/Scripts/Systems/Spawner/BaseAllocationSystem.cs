using FourCorners.Scripts.Components.Spawner;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Spawner
{
    /// <summary>
    /// Activates the PlayerBase and all of its owned Spawners for a newly connected player.
    ///
    /// Linking strategy (no TeamNumber on spawners):
    ///   1. Find the PlayerBase whose TeamNumber == PendingBaseAllocation.ApprovedTeam.
    ///   2. Set PlayerBase.IsActive = true, PlayerBase.NetworkId = playerId.
    ///   3. Scan all SpawnerData for entries where spawner.PlayerBaseEntity == baseEntity.
    ///      Set their NetworkId = playerId and IsActive = true (ghost field for client replication).
    ///      SpawnerSystem independently gates on PlayerBase.IsActive, so IsActive here is
    ///      a derived mirror — not the authority.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BaseAllocationSystem : ISystem
    {
        private EntityQuery _baseQuery;
        private EntityQuery _spawnerQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId, NetworkStreamInGame, PendingBaseAllocation>();
            state.RequireForUpdate(state.GetEntityQuery(builder));

            _baseQuery = state.GetEntityQuery(ComponentType.ReadWrite<PlayerBase>());
            _spawnerQuery = state.GetEntityQuery(ComponentType.ReadWrite<SpawnerData>());

            state.RequireForUpdate(_baseQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var bases = _baseQuery.ToEntityArray(Allocator.Temp);
            var spawners = _spawnerQuery.ToEntityArray(Allocator.Temp);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (networkId, allocation, connectionEntity) in
                     SystemAPI.Query<RefRO<NetworkId>, RefRO<PendingBaseAllocation>>()
                         .WithAll<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                int playerId = networkId.ValueRO.Value;
                var approvedTeam = allocation.ValueRO.ApprovedTeam;
                bool assigned = false;
                Entity baseEntity = Entity.Null;

                // --- Phase 1: Activate the PlayerBase ---
                foreach (var candidate in bases)
                {
                    var baseData = state.EntityManager.GetComponentData<PlayerBase>(candidate);

                    if (baseData.TeamNumber == approvedTeam && !baseData.IsActive)
                    {
                        baseData.IsActive = true;
                        baseData.NetworkId = playerId;
                        state.EntityManager.SetComponentData(candidate, baseData);

                        baseEntity = candidate;
                        assigned = true;

                        UnityEngine.Debug.Log(
                            $"[BaseAllocationSystem] Activated PlayerBase Team={approvedTeam} for NetworkId={playerId}");
                        break;
                    }
                }

                // --- Phase 2: Activate owned Spawners by entity reference ---
                // No TeamNumber scan — each spawner carries its owning base entity directly.
                if (assigned)
                {
                    foreach (var spawnerEntity in spawners)
                    {
                        var spawnerData = state.EntityManager.GetComponentData<SpawnerData>(spawnerEntity);

                        if (spawnerData.PlayerBaseEntity == baseEntity)
                        {
                            spawnerData.NetworkId = playerId;
                            spawnerData.IsActive = true; // mirror for client ghost replication
                            state.EntityManager.SetComponentData(spawnerEntity, spawnerData);
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError(
                        $"[BaseAllocationSystem] No inactive base found for Team={approvedTeam} NetworkId={playerId}. " +
                        "Verify that the PlayerBase in the sub-scene has the correct TeamNumber baked.");
                }

                ecb.RemoveComponent<PendingBaseAllocation>(connectionEntity);
            }

            bases.Dispose();
            spawners.Dispose();
        }
    }
}