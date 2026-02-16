using System.Collections.Generic;
using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;
using UnityEngine;

namespace ElementLogicFail.Scripts.Authoring.Spawner
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public ElementType type;
        public GameObject prefab;
        public float spawnRate;
        public int initialPoolSize;
        public List<Transform> Waypoints;

        public class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Components.Spawner.Spawner
                {
                    Type = authoring.type,
                    ElementPrefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                    SpawnRate = authoring.spawnRate,
                    Timer = 0
                });
                
                AddComponent(entity, new Components.Pool.ElementPool
                {
                    ElementType = (int)authoring.type,
                    Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                    PoolSize = authoring.initialPoolSize
                });

                var registryEntity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(registryEntity, new Components.Spawner.SpawnerRegistry
                {
                    Type = authoring.type,
                    SpawnerEntity = entity
                });

                AddBuffer<Components.Request.ElementSpawnRequest>(entity);
                
                var buffer = AddBuffer<Components.Path.PathWaypoint>(entity);
                if (authoring.Waypoints != null)
                {
                    foreach (var wp in authoring.Waypoints)
                    {
                        if (wp != null)
                        {
                            buffer.Add(new Components.Path.PathWaypoint { Position = wp.position });
                        }
                    }
                }
            }
        }
    }
}