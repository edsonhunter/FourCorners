using Unity.NetCode;
using UnityEngine.Scripting;

namespace FourCorners.Scripts.Bootstrapper
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
