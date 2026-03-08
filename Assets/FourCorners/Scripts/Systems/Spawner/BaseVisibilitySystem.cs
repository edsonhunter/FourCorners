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
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct BaseVisibilitySystem : ISystem
    {
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerBase>();
            _linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _linkedEntityGroupLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (playerBase, entity) in SystemAPI.Query<RefRO<PlayerBase>>().WithEntityAccess())
            {
                bool shouldBeHidden = !playerBase.ValueRO.IsActive;
                bool hasDisableRendering = SystemAPI.HasComponent<DisableRendering>(entity);

                if (shouldBeHidden && !hasDisableRendering)
                {
                    // Hide root entity
                    ecb.AddComponent<DisableRendering>(entity);

                    // Also hide all children via LinkedEntityGroup
                    if (_linkedEntityGroupLookup.TryGetBuffer(entity, out var linkedGroup))
                    {
                        for (int i = 0; i < linkedGroup.Length; i++)
                        {
                            var child = linkedGroup[i].Value;
                            if (child != entity)
                                ecb.AddComponent<DisableRendering>(child);
                        }
                    }
                }
                else if (!shouldBeHidden && hasDisableRendering)
                {
                    // Show root entity
                    ecb.RemoveComponent<DisableRendering>(entity);

                    // Also show all children via LinkedEntityGroup
                    if (_linkedEntityGroupLookup.TryGetBuffer(entity, out var linkedGroup))
                    {
                        for (int i = 0; i < linkedGroup.Length; i++)
                        {
                            var child = linkedGroup[i].Value;
                            if (child != entity)
                                ecb.RemoveComponent<DisableRendering>(child);
                        }
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
