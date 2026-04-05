using FourCorners.Scripts.Components.Team;
using Unity.Entities;
using UnityEngine;

namespace FourCorners.Scripts.Authoring.Network
{
    /// <summary>
    /// Place this on a single GameObject in the Server sub-scene.
    /// The Baker creates the MatchStateTag entity and pre-seeds exactly 4
    /// TeamStatusElement entries — one per corner/team — all unoccupied.
    /// ServerAcceptGameSystem finds this entity via SystemAPI.GetSingletonBuffer.
    /// </summary>
    public class MatchStateAuthoring : MonoBehaviour
    {
        public class MatchStateBaker : Baker<MatchStateAuthoring>
        {
            public override void Bake(MatchStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent<MatchStateTag>(entity);

                var buffer = AddBuffer<TeamStatusElement>(entity);

                // Seed exactly 4 slots — one for each TeamNumber value (0-3)
                buffer.Add(new TeamStatusElement { IsOccupied = false, OccupyingPlayer = Entity.Null });
                buffer.Add(new TeamStatusElement { IsOccupied = false, OccupyingPlayer = Entity.Null });
                buffer.Add(new TeamStatusElement { IsOccupied = false, OccupyingPlayer = Entity.Null });
                buffer.Add(new TeamStatusElement { IsOccupied = false, OccupyingPlayer = Entity.Null });
            }
        }
    }
}
