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
        private ComponentLookup<SpawnerRegistry> _spawnerRegistryLookup;
        private ComponentLookup<ParticlePrefabs>  _particlePrefabLookup;
        private ComponentLookup<SourcePool> _sourcePoolLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SimulationSingleton>();
            
            _elementLookup = SystemAPI.GetComponentLookup<ElementData>(true);
            _localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _spawnerRegistryLookup = SystemAPI.GetComponentLookup<SpawnerRegistry>(true);
            _particlePrefabLookup = SystemAPI.GetComponentLookup<ParticlePrefabs>(true);
            _sourcePoolLookup = SystemAPI.GetComponentLookup<SourcePool>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _elementLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _spawnerRegistryLookup.Update(ref state);
            _particlePrefabLookup.Update(ref state);
            _sourcePoolLookup.Update(ref state);

            bool hasParticles = SystemAPI.TryGetSingletonEntity<ParticlePrefabs>(out var particleManagerEntity);
            var particlePrefabLookup = SystemAPI.GetComponentLookup<ParticlePrefabs>(true);
            
            var typeToSpawnerMap = new NativeParallelHashMap<int, Entity>(16, Allocator.TempJob);
            foreach (var (registry, entity) in SystemAPI.Query<RefRO<SpawnerRegistry>>().WithEntityAccess())
            {
                typeToSpawnerMap[(int)registry.ValueRO.Type] = registry.ValueRO.SpawnerEntity;
            }
            
            SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            EndSimulationEntityCommandBufferSystem.Singleton endSimulationEntityCommandBufferSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer.ParallelWriter parallelWriter = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            var job = new CollisionEventJob
            {
                ElementLookup = _elementLookup,
                LocalTransformLookup = _localTransformLookup,
                TypeToSpawnerMap = typeToSpawnerMap,
                ParticlePrefabLookup = particlePrefabLookup,
                ParticleManagerEntity = particleManagerEntity,
                HasParticle = hasParticles,
                EntityCommandBuffer = parallelWriter,
                SourcePoolLookup = _sourcePoolLookup,
            };
            
            state.Dependency = job.Schedule(simulation, state.Dependency);
            typeToSpawnerMap.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
    
    public struct CollisionEventJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<ElementData> ElementLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [ReadOnly] public NativeParallelHashMap<int, Entity>  TypeToSpawnerMap;
        [ReadOnly] public ComponentLookup<ParticlePrefabs> ParticlePrefabLookup;
        [ReadOnly] public Entity ParticleManagerEntity;
        [ReadOnly] public bool HasParticle;
        [ReadOnly] public ComponentLookup<SourcePool> SourcePoolLookup;
        
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

            float3 position = 0.5f * (LocalTransformLookup[a].Position + LocalTransformLookup[b].Position);
            ParticlePrefabs particlePrefabs = HasParticle ? ParticlePrefabLookup[ParticleManagerEntity] : default;

            if (dataA.Type == dataB.Type)
            {
                return;
            }

            var poolEntityA = SourcePoolLookup[a].PoolEntity;
            var poolEntityB = SourcePoolLookup[b].PoolEntity;

            EntityCommandBuffer.AddComponent<Disabled>(0, a);
            EntityCommandBuffer.AddComponent<Disabled>(0, b);
            
            EntityCommandBuffer.AppendToBuffer(0, poolEntityA, new PooledEntity { Value = a });
            EntityCommandBuffer.AppendToBuffer(0, poolEntityB, new PooledEntity { Value = b });
            
            AppendParticleRequest(particlePrefabs.ParticlePrefab, position);
        }

        private void AppendParticleRequest(Entity particlePrefab, float3 position)
        {
            if (!HasParticle)
            {
                return;
            }
            
            EntityCommandBuffer.AppendToBuffer(0, ParticleManagerEntity, new ParticleSpawnRequest
            {
                Prefab = particlePrefab,
                Position = position,
            });
        }
    }
}