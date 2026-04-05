using FourCorners.Scripts.Components.Team;
using Unity.Entities;
using UnityEngine;

namespace FourCorners.Scripts.Authoring.Network
{
    /// <summary>
    /// OBSOLETE — Do NOT use. Replaced by MatchStateBootstrapSystem.
    /// The MatchState entity and DynamicBuffer[TeamStatusElement] are now created
    /// automatically at runtime by MatchStateBootstrapSystem during InitializationSystemGroup.
    /// No Server sub-scene or manual GameObject setup is required.
    /// </summary>
    [System.Obsolete("Replaced by MatchStateBootstrapSystem. Safe to delete this file.")]
    public class MatchStateAuthoring : MonoBehaviour
    {
        public class MatchStateBaker : Baker<MatchStateAuthoring>
        {
            public override void Bake(MatchStateAuthoring authoring)
            {
                // No-op: MatchStateBootstrapSystem handles this at runtime.
            }
        }
    }
}
