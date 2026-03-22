using ElementLogicFail.Scripts.Components.Element;
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
        private ComponentLookup<ElementData> _elementLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private NativeHashSet<Entity> _processedEntities;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SimulationSingleton>();
            
            _elementLookup = SystemAPI.GetComponentLookup<ElementData>(true);
            _localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _processedEntities = new NativeHashSet<Entity>(128, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _elementLookup.Update(ref state);
            _localTransformLookup.Update(ref state);

            _processedEntities.Clear();
            
            SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            EndSimulationEntityCommandBufferSystem.Singleton endSimulationEntityCommandBufferSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer.ParallelWriter parallelWriter = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            var job = new CollisionEventJob
            {
                ElementLookup = _elementLookup,
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
        [ReadOnly] public ComponentLookup<ElementData> ElementLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        public NativeHashSet<Entity> ProcessedEntities;
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity a = collisionEvent.EntityA;
            Entity b = collisionEvent.EntityB;

            if (!ElementLookup.HasComponent(a) || !ElementLookup.HasComponent(b))
            {
                return;
            }
            
            var dataA = ElementLookup[a];
            var dataB = ElementLookup[b];

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