using System;
using ElementLogicFail.Scripts.Domain;
using ElementLogicFail.Scripts.Domain.Interface;
using NUnit.Framework;
using Random = UnityEngine.Random;

namespace ElementLogicFail.Scripts.Tests
{
    public class MinionsTests
    {
        private IMinion CreateMinion(MinionType type)
        {
            return new Minion(type);
        }

        private IMinion CreateRandomMinion()
        {
            var rand = Random.Range(0, Enum.GetValues(typeof(MinionType)).Length);
            return new Minion((MinionType)rand);
        }

        [Test]
        public void CheckTypes()
        {
            var water1 = CreateMinion(MinionType.Water);
            Assert.That(water1.Type, Is.EqualTo(MinionType.Water));
            var water2 = CreateMinion(MinionType.Water);
            Assert.True(water1.IsSameType(water2));
            var fire1 = CreateMinion(MinionType.Fire);
            Assert.False(fire1.IsSameType(water1));
        }
        
        [Test]
        public void CheckRandomTypes()
        {
            var minion1 = CreateRandomMinion();
            var minion2 = CreateRandomMinion();
            var isSameType = minion1.IsSameType(minion2);
            if (isSameType)
            {
                Assert.True(minion1.IsSameType(minion2));
            }
            else
            {
                Assert.False(minion1.IsSameType(minion2));
            }
        }
    }
}