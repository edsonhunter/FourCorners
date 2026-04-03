using ElementLogicFail.Scripts.Components.Minion;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Components.Request
{
    public struct SpawnMinionRpc : IRpcCommand
    {
        public UnitModelType ModelType;
    }
}
