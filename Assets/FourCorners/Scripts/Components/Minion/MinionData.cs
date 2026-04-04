using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Minion
{
    public struct MinionData : IComponentData
    {
        [GhostField] public Team Team;
        [GhostField] public TeamColor TeamColor;
        [GhostField] public float Speed;
        [GhostField] public float3 Target;
        [GhostField] public uint RandomSeed;
        public float Cooldown;
    }
}
