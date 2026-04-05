using System.Collections.Generic;
using FourCorners.Scripts.Components.Minion;
using FourCorners.Scripts.Components.Team;
using Unity.Entities;
using UnityEngine;

namespace FourCorners.Scripts.Authoring.Spawner
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public TeamNumber type;
        public List<UnitModelType> UnitsToSpawn;
        public int spawnAmount = 5;
        public float spawnInterval = 2.0f;
        public List<Transform> Waypoints;

        public class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Components.Spawner.SpawnerData
                {
                    TeamNumber = authoring.type,
                    SpawnAmount = authoring.spawnAmount,
                    SpawnInterval = authoring.spawnInterval,
                    Timer = 0,
                    IsActive = false
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

                AddBuffer<Components.Request.MinionSpawnRequest>(entity);
                
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
