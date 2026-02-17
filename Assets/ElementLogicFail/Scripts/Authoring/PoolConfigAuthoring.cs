using System;
using System.Collections.Generic;
using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Pool;
using Unity.Entities;
using UnityEngine;

namespace ElementLogicFail.Scripts.Authoring
{
    public class PoolConfigAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct PoolDefinition
        {
            public GameObject Prefab;
            public int InitialCount;
            public UnitModelType ModelType;
        }

        public List<PoolDefinition> Pools;

        public class PoolConfigBaker : Baker<PoolConfigAuthoring>
        {
            public override void Bake(PoolConfigAuthoring authoring)
            {
                if (authoring.Pools == null) return;

                foreach (var poolDef in authoring.Pools)
                {
                    if (poolDef.Prefab == null) continue;

                    var prefabEntity = GetEntity(poolDef.Prefab, TransformUsageFlags.Dynamic);

                    // Create a dedicated Pool Entity for this prefab
                    // We don't attach this to the Authoring Entity itself to avoid clutter
                    var poolEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                    AddComponent(poolEntity, new ElementPool
                    {
                        ModelType = poolDef.ModelType,
                        Prefab = prefabEntity,
                        PoolSize = poolDef.InitialCount
                    });

                    // Add the buffer to store the actual pooled entities
                    AddBuffer<PooledEntity>(poolEntity);
                }
            }
        }
    }
}
