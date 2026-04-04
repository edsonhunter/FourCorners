using FourCorners.Scripts.Components.Minion;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Spawner
{
    public struct PlayerBase : IComponentData
    {
        public Team Team;
        [GhostField] public bool IsActive;
        [GhostField] public int NetworkId;
    }
}
