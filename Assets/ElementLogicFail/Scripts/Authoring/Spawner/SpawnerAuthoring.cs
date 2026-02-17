using System.Collections.Generic;
using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;
using UnityEngine;

namespace ElementLogicFail.Scripts.Authoring.Spawner
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public Team type;
        public List<UnitModelType> UnitsToSpawn;
        public float spawnRate;
        public List<Transform> Waypoints;

        public class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Components.Spawner.Spawner
                {
                    Team = authoring.type,
                    SpawnRate = authoring.spawnRate,
                    Timer = 0
                });
                
                // Add Unit Types to Spawn
                if (authoring.UnitsToSpawn != null)
                {
                    var prefabBuffer = AddBuffer<Components.Spawner.SpawnerPrefab>(entity);
                    foreach (var unitType in authoring.UnitsToSpawn)
                    {
                        prefabBuffer.Add(new Components.Spawner.SpawnerPrefab { ModelType = unitType });
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