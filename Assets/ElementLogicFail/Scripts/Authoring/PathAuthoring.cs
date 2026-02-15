using System.Collections.Generic;
using ElementLogicFail.Scripts.Components.Path;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ElementLogicFail.Scripts.Authoring
{
    public class PathAuthoring : MonoBehaviour
    {
        public List<Transform> Waypoints;

        public class PathAuthoringBaker : Baker<PathAuthoring>
        {
            public override void Bake(PathAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.Waypoints == null || authoring.Waypoints.Count == 0) return;

                AddComponent(entity, new PathFollower
                {
                    CurrentIndex = 0,
                    IsReverse = false
                });

                var buffer = AddBuffer<PathWaypoint>(entity);
                foreach (var wp in authoring.Waypoints)
                {
                    if (wp != null)
                    {
                        buffer.Add(new PathWaypoint { Position = wp.position });
                    }
                }
            }
        }
    }
}
