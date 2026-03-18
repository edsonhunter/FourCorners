using ElementLogicFail.Scripts.Components.Element;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ElementSpawningSystem))]
    public partial struct ElementDestructionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new DestructionJob
            {
                Ecb = ecb
            };
            
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [BurstCompile]
    public partial struct DestructionJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRO<ElementData> data, RefRO<ElementDestructionTag> destructionTag)
        {
            Ecb.DestroyEntity(sortKey, entity);
        }
    }
}