using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

namespace ElementLogicFail.Scripts.Bootstrapper
{
    [Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            // Required for NetCode: without this, the Editor pauses when it loses focus,
            // causing a multi-frame time spike that NetworkTimeSystem interprets as a
            // large tick rollback (the "serverTick rolled back by -18 ticks" error).
            Application.runInBackground = true;

            return base.Initialize(defaultWorldName);
        }
    }
}

