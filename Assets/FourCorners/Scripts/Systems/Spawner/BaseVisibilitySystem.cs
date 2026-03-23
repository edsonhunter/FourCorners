using ElementLogicFail.Scripts.Components.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    /// <summary>
    /// Hides/shows PlayerBase entities based on IsActive.
    /// Because bases likely have child mesh entities, we also traverse LinkedEntityGroup
    /// to apply DisableRendering to ALL child entities, not just the root.
    /// Runs on clients only (PresentationSystemGroup doesn't exist on server).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct BaseVisibilitySystem : ISystem
    {
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;
        private ComponentLookup<DisableRendering> _disableRenderingLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerBase>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
            _disableRenderingLookup = state.GetComponentLookup<DisableRendering>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _linkedEntityGroupLookup.Update(ref state);
            _disableRenderingLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new UpdateVisibilityJob
            {
                Ecb = ecb,
                LinkedGroupLookup = _linkedEntityGroupLookup,
                DisableRenderingLookup = _disableRenderingLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct UpdateVisibilityJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedGroupLookup;
        [ReadOnly] public ComponentLookup<DisableRendering> DisableRenderingLookup;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRO<PlayerBase> playerBase)
        {
            bool shouldBeHidden = !playerBase.ValueRO.IsActive;
            bool hasDisableRendering = DisableRenderingLookup.HasComponent(entity);

            if (shouldBeHidden && !hasDisableRendering)
            {
                // Hide root entity
                Ecb.AddComponent<DisableRendering>(sortKey, entity);

                // Also hide all children via LinkedEntityGroup
                if (LinkedGroupLookup.TryGetBuffer(entity, out var linkedGroup))
                {
                    for (int i = 0; i < linkedGroup.Length; i++)
                    {
                        var child = linkedGroup[i].Value;
                        if (child != entity)
                            Ecb.AddComponent<DisableRendering>(sortKey, child);
                    }
                }
            }
            else if (!shouldBeHidden && hasDisableRendering)
            {
                // Show root entity
                Ecb.RemoveComponent<DisableRendering>(sortKey, entity);

                // Also show all children via LinkedEntityGroup
                if (LinkedGroupLookup.TryGetBuffer(entity, out var linkedGroup))
                {
                    for (int i = 0; i < linkedGroup.Length; i++)
                    {
                        var child = linkedGroup[i].Value;
                        if (child != entity)
                            Ecb.RemoveComponent<DisableRendering>(sortKey, child);
                    }
                }
            }
        }
    }
}
