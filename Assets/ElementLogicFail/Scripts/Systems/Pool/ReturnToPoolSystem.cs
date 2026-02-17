using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Pool;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;

namespace ElementLogicFail.Scripts.Systems.Pool
{
    
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial struct ReturnToPoolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ElementPool>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var returnJob = new ReturnJob
            {
                Ecb = ecb
            };
            
            state.Dependency = returnJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    [BurstCompile]
    public partial struct ReturnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRO<ElementData> data, RefRO<ReturnToPool> returnTag, RefRO<SourcePool> sourcePool)
        {
            var poolEntity = sourcePool.ValueRO.PoolEntity;
            
            Ecb.AddComponent<Disabled>(sortKey, entity);
            Ecb.AppendToBuffer(sortKey, poolEntity, new PooledEntity
            {
                Value = entity
            });
            
            Ecb.RemoveComponent<ReturnToPool>(sortKey, entity);
        }
    }
}