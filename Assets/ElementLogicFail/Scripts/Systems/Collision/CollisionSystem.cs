using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Particles;
using ElementLogicFail.Scripts.Components.Pool;
using ElementLogicFail.Scripts.Components.Request;
using ElementLogicFail.Scripts.Components.Spawner;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Collision
{
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct CollisionSystem : ISystem
    {
        private ComponentLookup<ElementData> _elementLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<SourcePool> _sourcePoolLookup;
        private NativeHashSet<Entity> _processedEntities;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SimulationSingleton>();
            
            _elementLookup = SystemAPI.GetComponentLookup<ElementData>(true);
            _localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _sourcePoolLookup = SystemAPI.GetComponentLookup<SourcePool>(true);
            _processedEntities = new NativeHashSet<Entity>(128, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _elementLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _sourcePoolLookup.Update(ref state);

            _processedEntities.Clear();

            bool hasParticles = SystemAPI.TryGetSingletonEntity<ParticlePrefabs>(out var particleManagerEntity);
            var particlePrefabLookup = SystemAPI.GetComponentLookup<ParticlePrefabs>(true);
            
            SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            EndSimulationEntityCommandBufferSystem.Singleton endSimulationEntityCommandBufferSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer.ParallelWriter parallelWriter = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            var job = new CollisionEventJob
            {
                ElementLookup = _elementLookup,
                LocalTransformLookup = _localTransformLookup,
                ParticlePrefabLookup = particlePrefabLookup,
                ParticleManagerEntity = particleManagerEntity,
                HasParticle = hasParticles,
                EntityCommandBuffer = parallelWriter,
                SourcePoolLookup = _sourcePoolLookup,
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
        [ReadOnly] public ComponentLookup<ParticlePrefabs> ParticlePrefabLookup;
        [ReadOnly] public Entity ParticleManagerEntity;
        [ReadOnly] public bool HasParticle;
        [ReadOnly] public ComponentLookup<SourcePool> SourcePoolLookup;
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
                AppendEntityRequest(a);
            }

            if (canDisableB)
            {
                AppendEntityRequest(b);
            }
            
            if (HasParticle)
            {
                var particlePrefabs = ParticlePrefabLookup[ParticleManagerEntity];
                AppendParticleRequest(particlePrefabs.ParticlePrefab, position);
            }
        }

        private void AppendEntityRequest(Entity entity)
        {
            ProcessedEntities.Add(entity);
            var poolEntity = SourcePoolLookup[entity].PoolEntity;
            EntityCommandBuffer.AddComponent<Disabled>(0, entity);
            EntityCommandBuffer.AppendToBuffer(0, poolEntity, new PooledEntity { Value = entity });
        }

        private void AppendParticleRequest(Entity particlePrefab, float3 position)
        {
            EntityCommandBuffer.AppendToBuffer(0, ParticleManagerEntity, new ParticleSpawnRequest
            {
                Prefab = particlePrefab,
                Position = position,
            });
        }
    }
}