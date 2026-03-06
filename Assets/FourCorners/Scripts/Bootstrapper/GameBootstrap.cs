using Unity.NetCode;
using UnityEngine.Scripting;

namespace ElementLogicFail.Scripts.Bootstrapper
{
    [Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            AutoConnectPort = 7979;
            return base.Initialize(defaultWorldName);
        }
    }
}
