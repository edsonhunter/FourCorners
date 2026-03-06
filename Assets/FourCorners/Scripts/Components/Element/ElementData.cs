using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Components.Element
{
    public struct ElementData : IComponentData
    {
        [GhostField] public Team Team;
        [GhostField] public TeamColor TeamColor;
        public float Speed;
        public float3 Target;
        public uint RandomSeed;
        public float Cooldown;
    }
}