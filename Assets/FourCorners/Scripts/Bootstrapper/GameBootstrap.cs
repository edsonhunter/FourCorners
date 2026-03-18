using Unity.NetCode;
using UnityEngine.Scripting;

namespace ElementLogicFail.Scripts.Bootstrapper
{
    [Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            return base.Initialize(defaultWorldName);
        }
    }
}
