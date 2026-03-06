using ElementLogicFail.Scripts.Components.Element;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Components.Request
{
    public struct SpawnMinionRpc : IRpcCommand
    {
        public UnitModelType ModelType;
    }
}
