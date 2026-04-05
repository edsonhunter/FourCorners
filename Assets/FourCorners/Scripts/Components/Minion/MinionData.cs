using FourCorners.Scripts.Components.Team;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Minion
{
    public struct MinionData : IComponentData
    {
        [GhostField] public Team.TeamNumber TeamNumber;
        [GhostField] public TeamColor TeamColor;
        [GhostField] public float Speed;
        [GhostField] public float3 Target;
        [GhostField] public uint RandomSeed;
        public float Cooldown;
    }
}
