using FourCorners.Scripts.Components.Minion;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace FourCorners.Scripts.Systems.Collision
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CooldownSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            foreach (RefRW<MinionData> minionData in SystemAPI.Query<RefRW<MinionData>>())
            {
                if (minionData.ValueRO.Cooldown > 0f)
                {
                    minionData.ValueRW.Cooldown = math.max(0f, minionData.ValueRO.Cooldown - deltaTime);
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
