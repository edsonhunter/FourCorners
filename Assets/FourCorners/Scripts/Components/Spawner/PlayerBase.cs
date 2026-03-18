using ElementLogicFail.Scripts.Components.Element;
using Unity.Entities;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Components.Spawner
{
    public struct PlayerBase : IComponentData
    {
        public Team Team;
        [GhostField] public bool IsActive;
        [GhostField] public int NetworkId;
    }
}
