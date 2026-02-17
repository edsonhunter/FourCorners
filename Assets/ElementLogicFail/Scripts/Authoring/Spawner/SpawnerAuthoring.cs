using System.Collections.Generic;
using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;
using UnityEngine;

namespace ElementLogicFail.Scripts.Authoring.Spawner
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public ElementType type;
        public List<GameObject> prefabs;
        public float spawnRate;
        public List<Transform> Waypoints;

        public class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Components.Spawner.Spawner
                {
                    Type = authoring.type,
                    SpawnRate = authoring.spawnRate,
                    Timer = 0
                });
                
                // Create Pool for EACH prefab
                if (authoring.prefabs != null)
                {
                    var prefabBuffer = AddBuffer<Components.Spawner.SpawnerPrefab>(entity);
                    foreach (var prefabGo in authoring.prefabs)
                    {
                        if (prefabGo == null) continue;
                        var prefabEntity = GetEntity(prefabGo, TransformUsageFlags.Dynamic);
                        
                        prefabBuffer.Add(new Components.Spawner.SpawnerPrefab { Prefab = prefabEntity });
                    }
                }

                // Create Registry Entity for Collision System lookup
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