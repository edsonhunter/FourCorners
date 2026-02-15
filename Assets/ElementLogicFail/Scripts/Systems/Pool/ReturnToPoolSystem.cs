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
        private NativeParallelHashMap<int, Entity> _typeToPool;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ElementPool>();
            _typeToPool = new NativeParallelHashMap<int, Entity>(16, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _typeToPool.Clear();
            
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var buildMapJob = new BuildTypeToPoolMapJob
            {
                TypeToPool = _typeToPool
            };
            
            state.Dependency = buildMapJob.Schedule(state.Dependency);

            var returnJob = new ReturnJob
            {
                TypeToPool = _typeToPool,
                Ecb = ecb
            };
            
            state.Dependency = returnJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_typeToPool.IsCreated)
                _typeToPool.Dispose();
        }
    }

    [BurstCompile]
    public partial struct BuildTypeToPoolMapJob : IJobEntity
    {
        public NativeParallelHashMap<int, Entity> TypeToPool;

        private void Execute(Entity entity, RefRO<ElementPool> pool)
        {
            if (!TypeToPool.ContainsKey(pool.ValueRO.ElementType))
            {
                TypeToPool.Add(pool.ValueRO.ElementType, entity);
            }
        }
    }

    [BurstCompile]
    public partial struct ReturnJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<int, Entity> TypeToPool;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRO<ElementData> data, RefRO<ReturnToPool> returnTag)
        {
            if (TypeToPool.TryGetValue((int)data.ValueRO.Type, out var poolEntity))
            {
                Ecb.AddComponent<Disabled>(sortKey, entity);
                Ecb.AppendToBuffer(sortKey, poolEntity, new PooledEntity
                {
                    Value = entity
                });
            }
            else
            {
                Ecb.DestroyEntity(sortKey, entity);
            }
            Ecb.RemoveComponent<ReturnToPool>(sortKey, entity);
        }
    }
}