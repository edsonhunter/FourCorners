using FourCorners.Scripts.Components.Minion;
using Unity.NetCode;

namespace FourCorners.Scripts.Components.Request
{
    public struct SpawnMinionRpc : IRpcCommand
    {
        public UnitModelType ModelType;
    }
}
