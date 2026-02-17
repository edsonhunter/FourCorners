using Unity.Entities;
using Unity.Mathematics;

namespace ElementLogicFail.Scripts.Components.Element
{
    public struct ElementData : IComponentData
    {
        public Team Team;
        public float Speed;
        public float3 Target;
        public uint RandomSeed;
        public float Cooldown;
    }
}