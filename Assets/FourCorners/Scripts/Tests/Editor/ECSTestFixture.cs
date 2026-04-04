using NUnit.Framework;
using Unity.Entities;

namespace FourCorners.Scripts.Tests.Editor
{
    public class ECSTestFixture
    {
        protected World World;
        protected EntityManager EntityManager;

        [SetUp]
        public virtual void Setup()
        {
            World = new World("ECSTestSetup");
            var entitySimulationCommandBufferSystem =
                World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            EntityManager = entitySimulationCommandBufferSystem.EntityManager;
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (World != null && World.IsCreated)
            {
                World.Dispose();
            }
        }
    }
}
