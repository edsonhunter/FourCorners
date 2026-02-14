using ElementLogicFail.Scripts.Components.Particles;
using ElementLogicFail.Scripts.Components.Pool;
using ElementLogicFail.Scripts.Components.Request;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace ElementLogicFail.Scripts.Systems.Particles
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ParticleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EndSimulationEntityCommandBufferSystem.Singleton entitySimulationCommandBufferSystem =
                SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer entityCommandBuffer = entitySimulationCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);

            NativeParallelHashMap<Entity, Entity> prefabToPoolMap = new NativeParallelHashMap<Entity, Entity>(16, Allocator.TempJob);

            foreach (var (pool, entity) in SystemAPI.Query<RefRO<ParticlePool>>().WithEntityAccess())
            {
                if (pool.ValueRO.Prefab != Entity.Null)
                {
                    prefabToPoolMap.TryAdd(pool.ValueRO.Prefab, entity);
                }
            }

            foreach (var (request, entity) in SystemAPI
                         .Query<DynamicBuffer<ParticleSpawnRequest>>().WithEntityAccess())
            {
                foreach (var spawnRequest in request)
                {
                    Entity instance;
                    if (prefabToPoolMap.TryGetValue(spawnRequest.Prefab, out Entity poolEntity))
                    {
                        var poolBuffer = SystemAPI.GetBuffer<PooledEntity>(poolEntity);
                        if (poolBuffer.Length > 0)
                        {
                            instance = poolBuffer[poolBuffer.Length - 1].Value;
                            poolBuffer.RemoveAt(poolBuffer.Length - 1);

                            entityCommandBuffer.RemoveComponent<Disabled>(instance);
                            entityCommandBuffer.SetComponent(instance, LocalTransform.FromPosition(spawnRequest.Position));
                            entityCommandBuffer.SetComponent(instance, new ParticleEffectData
                            {
                                Lifetime = 1f,
                                Timer = 0f
                            });
                        }
                        else
                        {
                            instance = entityCommandBuffer.Instantiate(spawnRequest.Prefab);
                            entityCommandBuffer.AddComponent(instance, new ParentPool { PoolEntity = poolEntity });
                            entityCommandBuffer.SetComponent(instance, LocalTransform.FromPosition(spawnRequest.Position));
                            entityCommandBuffer.AddComponent(instance, new ParticleEffectData
                            {
                                Lifetime = 1f,
                                Timer = 0f
                            });
                        }
                    }
                    else
                    {
                        instance = entityCommandBuffer.Instantiate(spawnRequest.Prefab);
                        entityCommandBuffer.SetComponent(instance, LocalTransform.FromPosition(spawnRequest.Position));
                        entityCommandBuffer.AddComponent(instance, new ParticleEffectData
                        {
                            Lifetime = 1f,
                            Timer = 0f
                        });
                    }
                }
                request.Clear();
            }
            
            state.Dependency = prefabToPoolMap.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
