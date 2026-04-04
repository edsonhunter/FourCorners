using FourCorners.Scripts.Components.Minion;
using FourCorners.Scripts.Systems.Collision;
using FourCorners.Scripts.Tests.Editor;
using NUnit.Framework;
using Unity.Entities;

namespace FourCorners.Scripts.Tests.Systems
{
    [TestFixture]
    public class CooldownSystemTest : ECSTestFixture
    {
        [Test]
        public void CooldownSystem_ReducesCooldownOverTime()
        {
            var entity = EntityManager.CreateEntity(typeof(MinionData));
            float initialCooldown = 5.0f;
            EntityManager.SetComponentData(entity, new MinionData { Cooldown = initialCooldown });
            
            World.GetOrCreateSystem<CooldownSystem>().Update(World.Unmanaged);
            
            var newCooldown = EntityManager.GetComponentData<MinionData>(entity).Cooldown;
            Assert.Less(newCooldown, initialCooldown);
        }

        [Test]
        public void CooldownSystem_DoesNotGoBelowZero()
        {
            var entity = EntityManager.CreateEntity(typeof(MinionData));
            EntityManager.SetComponentData(entity, new MinionData { Cooldown = 0.1f });

            var system = World.GetOrCreateSystem<CooldownSystem>();
            system.Update(World.Unmanaged);
            system.Update(World.Unmanaged);
            
            var newCooldown = EntityManager.GetComponentData<MinionData>(entity).Cooldown;
            Assert.AreEqual(0f, newCooldown);
        }
    }
}
