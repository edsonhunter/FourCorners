using System;
using ElementLogicFail.Scripts.Domain.Interface;

namespace ElementLogicFail.Scripts.Domain
{
    public class Minion : IMinion
    {
        public MinionType Type { get; }

        private Minion()
        {
            Type = MinionType.Unknown;
        }
        
        public Minion(MinionType type) : this()
        {
            Type = type;
        }
        
        public bool IsSameType(IMinion minion)
        {
            if (minion == null)
            {
                throw new InvalidOperationException($"minion object is null");
            }
            return minion.Type == Type;
        }
    }
}