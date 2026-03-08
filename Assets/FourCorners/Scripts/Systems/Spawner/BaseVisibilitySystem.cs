using ElementLogicFail.Scripts.Components.Spawner;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct BaseVisibilitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerBase>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);

            foreach (var (playerBase, entity) in SystemAPI.Query<RefRO<PlayerBase>>().WithEntityAccess())
            {
                bool hasDisableRendering = SystemAPI.HasComponent<DisableRendering>(entity);

                if (!playerBase.ValueRO.IsActive && !hasDisableRendering)
                {
                    ecb.AddComponent<DisableRendering>(entity);
                }
                else if (playerBase.ValueRO.IsActive && hasDisableRendering)
                {
                    ecb.RemoveComponent<DisableRendering>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
