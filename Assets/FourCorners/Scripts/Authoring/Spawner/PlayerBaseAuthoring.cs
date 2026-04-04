using FourCorners.Scripts.Components.Minion;
using Unity.Entities;
using UnityEngine;

namespace FourCorners.Scripts.Authoring.Spawner
{
    public class PlayerBaseAuthoring : MonoBehaviour
    {
        public Team Team;

        public class PlayerBaseAuthoringBaker : Baker<PlayerBaseAuthoring>
        {
            public override void Bake(PlayerBaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Components.Spawner.PlayerBase
                {
                    Team = authoring.Team,
                    IsActive = false,
                    NetworkId = 0
                });
            }
        }
    }
}
