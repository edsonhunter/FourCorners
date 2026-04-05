using FourCorners.Scripts.Components.Team;
using Unity.Entities;

namespace FourCorners.Scripts.Components.Spawner
{
    /// <summary>
    /// Added to a server-side connection entity after ServerAcceptGameSystem validates the
    /// team request. Carries the ApprovedTeam so BaseAllocationSystem can bind the player
    /// to the exact corner base — no sequential first-available fallback needed.
    /// </summary>
    public struct PendingBaseAllocation : IComponentData
    {
        public TeamNumber ApprovedTeam;
    }
}
