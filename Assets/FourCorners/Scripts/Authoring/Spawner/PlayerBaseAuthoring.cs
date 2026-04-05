using FourCorners.Scripts.Components.Team;
using Unity.Entities;
using UnityEngine;

namespace FourCorners.Scripts.Authoring.Spawner
{
    public class PlayerBaseAuthoring : MonoBehaviour
    {
        TeamNumber teamNumber;

        public class PlayerBaseAuthoringBaker : Baker<PlayerBaseAuthoring>
        {
            public override void Bake(PlayerBaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Components.Spawner.PlayerBase
                {
                    TeamNumber = authoring.teamNumber,
                    IsActive = false,
                    NetworkId = 0
                });
            }
        }
    }
}
