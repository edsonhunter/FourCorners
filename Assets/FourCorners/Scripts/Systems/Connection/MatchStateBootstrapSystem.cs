using FourCorners.Scripts.Components.Team;
using Unity.Collections;
using Unity.Entities;

namespace FourCorners.Scripts.Systems.Connection
{
    /// <summary>
    /// Runs once during server startup to guarantee the MatchState entity exists
    /// before ServerAcceptGameSystem processes any GoInGameRequest RPCs.
    ///
    /// Replaces the manual MatchStateAuthoring Baker — no Server sub-scene required.
    /// Idempotent: if a MatchStateTag entity already exists (e.g. from baking),
    /// this system disables itself immediately without creating a duplicate.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MatchStateBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // If a baked entity already carries MatchStateTag, do nothing
            var existing = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MatchStateTag>()
                .Build(ref state);

            if (!existing.IsEmpty)
            {
                state.Enabled = false;
                return;
            }

            // Create the canonical MatchState entity
            var matchStateEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<MatchStateTag>(matchStateEntity);

            var buffer = state.EntityManager.AddBuffer<TeamStatusElement>(matchStateEntity);

            // Seed 4 unoccupied slots — one per TeamNumber value (0-3)
            for (int i = 0; i < 4; i++)
            {
                buffer.Add(new TeamStatusElement
                {
                    IsOccupied = false,
                    OccupyingPlayer = Entity.Null
                });
            }

            UnityEngine.Debug.Log("[MatchStateBootstrapSystem] MatchState entity created with 4 team slots.");

            // One-shot: disable after initialization so it never runs again
            state.Enabled = false;
        }

        // OnUpdate intentionally empty — all work is done in OnCreate
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}