using FourCorners.Scripts.Components.Minion;
using FourCorners.Scripts.Components.Request;
using FourCorners.Scripts.Components.Spawner;
using FourCorners.Scripts.Components.Team;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace FourCorners.Scripts.Tests
{
    public static class EntityTest
    {
        public static Entity CreateMinion(EntityManager manager, TeamNumber type, float speed, int cooldown)
        {
            var entity = manager.CreateEntity();
            manager.SetComponentData(entity, new MinionData()
            {
                TeamNumber= type,
                Speed = speed,
                Cooldown = cooldown,
                Target = float3.zero,
                RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue)
            });
            manager.SetComponentData(entity, LocalTransform.FromPosition(new float3(0, 0, 0)));
            return entity;
        }

        public static MinionData CreateMinionData(TeamNumber type, float speed, float cooldown)
        {
            return new MinionData()
            {
                TeamNumber = type,
                Speed = speed,
                Cooldown = cooldown,
                Target = float3.zero,
                RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue)
            };
        }
        
        public static Entity CreateTestMinion(EntityManager entityManager, TeamNumber type, float cooldown, float3 position)
        {
            var entity = entityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(PhysicsCollider),
                typeof(PhysicsVelocity),
                typeof(MinionData));
            
            entityManager.AddComponentData(entity, new PhysicsMass
            {
                InverseMass = 1f,
                InverseInertia = new float3(1f, 1f, 1f),
                AngularExpansionFactor = 1f
            });
            
            var capsule = CapsuleCollider.Create(new CapsuleGeometry { Vertex0 = float3.zero, Vertex1 = new float3(0, 1, 0), Radius = 0.5f });
            entityManager.SetComponentData(entity, new LocalTransform { Position = position, Scale = 1 });
            entityManager.SetComponentData(entity, new PhysicsCollider { Value = capsule });
            entityManager.SetComponentData(entity, CreateMinionData(type, 2, cooldown));
            return entity;
        }
        
        public static Entity CreateTestSpawner(EntityManager entityManager, float spawnRate, float timer)
        {
            Entity entity = entityManager.CreateEntity(typeof(SpawnerData), typeof(LocalTransform), typeof(MinionSpawnRequest), typeof(SpawnerPrefab));
            entityManager.SetComponentData(entity, new SpawnerData
            {
                TeamNumber = TeamNumber.Team1,
                SpawnInterval = spawnRate,
                Timer = timer,
                IsActive = true,
                SpawnAmount = 1
            });
            entityManager.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
            
            var prefabs = entityManager.GetBuffer<SpawnerPrefab>(entity);
            prefabs.Add(new SpawnerPrefab { ModelType = UnitModelType.Warrior });
            
            return entity;
        }
    }
}
