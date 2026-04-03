using ElementLogicFail.Scripts.Components.Minion;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Physics.Systems;

namespace ElementLogicFail.Scripts.Systems.Collision
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct CollisionSystem : ISystem
    {
        private ComponentLookup<MinionData> _minionLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private NativeHashSet<Entity> _processedEntities;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SimulationSingleton>();
            
            _minionLookup = state.GetComponentLookup<MinionData>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _processedEntities = new NativeHashSet<Entity>(128, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _minionLookup.Update(ref state);
            _localTransformLookup.Update(ref state);

            _processedEntities.Clear();
            
            SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            EndSimulationEntityCommandBufferSystem.Singleton endSimulationEntityCommandBufferSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer.ParallelWriter parallelWriter = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            var job = new CollisionEventJob
            {
                MinionLookup = _minionLookup,
                LocalTransformLookup = _localTransformLookup,
                EntityCommandBuffer = parallelWriter,
                ProcessedEntities = _processedEntities
            };
            
            state.Dependency = job.Schedule(simulation, state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_processedEntities.IsCreated)
                _processedEntities.Dispose();
        }
    }
    
    public struct CollisionEventJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<MinionData> MinionLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        public NativeHashSet<Entity> ProcessedEntities;
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity a = collisionEvent.EntityA;
            Entity b = collisionEvent.EntityB;

            if (!MinionLookup.HasComponent(a) || !MinionLookup.HasComponent(b))
            {
                return;
            }
            
            var dataA = MinionLookup[a];
            var dataB = MinionLookup[b];

            if (dataA.Team == dataB.Team)
            {
                return;
            }

            bool canDisableA = !ProcessedEntities.Contains(a);
            bool canDisableB = !ProcessedEntities.Contains(b);

            if (!canDisableA && !canDisableB)
            {
                return;
            }

            float3 position = 0.5f * (LocalTransformLookup[a].Position + LocalTransformLookup[b].Position);

            if (canDisableA)
            {
                AppendEntityRequest(a, collisionEvent.BodyIndexA);
            }

            if (canDisableB)
            {
                AppendEntityRequest(b, collisionEvent.BodyIndexB);
            }
        }

        private void AppendEntityRequest(Entity entity, int sortKey)
        {
            ProcessedEntities.Add(entity);
            EntityCommandBuffer.DestroyEntity(sortKey, entity);
        }
    }
}