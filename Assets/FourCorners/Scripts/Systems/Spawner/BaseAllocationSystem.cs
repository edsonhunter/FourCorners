using FourCorners.Scripts.Components.Spawner;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Systems.Spawner
{
    /// <summary>
    /// Assigns an available PlayerBase and its Spawners to a connected player.
    ///
    /// Runs only after NetworkStreamInGame has been set (by ServerAcceptGameSystem),
    /// which causes Netcode to activate prespawned ghost entities (remove Disabled tag).
    /// Connections waiting for a base are marked with PendingBaseAllocation.
    ///
    /// IMPORTANT: Never queries for Disabled or Prefab entities — bases must be fully
    /// active before we modify them, to avoid prespawned ghost baseline mismatches.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BaseAllocationSystem : ISystem
    {
        private EntityQuery _baseQuery;
        private EntityQuery _spawnerQuery;

        public void OnCreate(ref SystemState state)
        {
            // Requires at least one connection waiting for a base
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId, NetworkStreamInGame, PendingBaseAllocation>();
            state.RequireForUpdate(state.GetEntityQuery(builder));

            // Also require PlayerBase entities to exist (they become active after NetworkStreamInGame is set)
            _baseQuery = state.GetEntityQuery(ComponentType.ReadWrite<PlayerBase>());
            state.RequireForUpdate(_baseQuery);

            _spawnerQuery = state.GetEntityQuery(ComponentType.ReadWrite<Components.Spawner.Spawner>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var bases = _baseQuery.ToEntityArray(Allocator.Temp);
            var spawners = _spawnerQuery.ToEntityArray(Allocator.Temp);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            int baseIndex = 0;

            foreach (var (networkId, entity) in
                SystemAPI.Query<RefRO<NetworkId>>()
                    .WithAll<NetworkStreamInGame, PendingBaseAllocation>()
                    .WithEntityAccess())
            {
                int playerId = networkId.ValueRO.Value;
                bool assigned = false;

                // Find the next unassigned (IsActive=false) base
                for (; baseIndex < bases.Length; baseIndex++)
                {
                    var baseEntity = bases[baseIndex];
                    var baseData = state.EntityManager.GetComponentData<PlayerBase>(baseEntity);

                    if (!baseData.IsActive)
                    {
                        // Assign base to this player
                        baseData.IsActive = true;
                        baseData.NetworkId = playerId;
                        state.EntityManager.SetComponentData(baseEntity, baseData);

                        // Activate all spawners for the same team
                        foreach (var spawnerEntity in spawners)
                        {
                            var spawnerData = state.EntityManager.GetComponentData<Components.Spawner.Spawner>(spawnerEntity);
                            if (spawnerData.Team == baseData.Team)
                            {
                                spawnerData.IsActive = true;
                                spawnerData.NetworkId = playerId;
                                state.EntityManager.SetComponentData(spawnerEntity, spawnerData);
                            }
                        }

                        baseIndex++;
                        assigned = true;
                        UnityEngine.Debug.Log($"[Server] Assigned base Team={baseData.Team} to NetworkId={playerId}");
                        break;
                    }
                }

                if (!assigned)
                    UnityEngine.Debug.LogWarning($"[Server] No available base for NetworkId={playerId}");

                // Remove the pending tag so this connection isn't processed again
                ecb.RemoveComponent<PendingBaseAllocation>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            bases.Dispose();
            spawners.Dispose();
        }
    }
}
