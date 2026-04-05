using FourCorners.Scripts.Components.Minion;
using Unity.Entities;
using Unity.Mathematics;

namespace FourCorners.Scripts.Components.Request
{
    public struct MinionSpawnRequest : IBufferElementData
    {
        public Team.TeamNumber Type;
        public UnitModelType ModelType;
        public float3 Position;
    }
}
