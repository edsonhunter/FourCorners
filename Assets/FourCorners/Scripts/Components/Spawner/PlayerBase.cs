using FourCorners.Scripts.Components.Minion;
using Unity.Entities;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Spawner
{
    [GhostComponent]
    public struct PlayerBase : IComponentData
    {
        public Team.TeamNumber TeamNumber;
        [GhostField] public bool IsActive;
        [GhostField] public int NetworkId;
    }
}
