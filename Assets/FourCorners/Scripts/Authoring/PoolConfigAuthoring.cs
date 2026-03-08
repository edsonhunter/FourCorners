using System;
using System.Collections.Generic;
using ElementLogicFail.Scripts.Components.Element;
using ElementLogicFail.Scripts.Components.Pool;
using Unity.Entities;
using UnityEngine;

using Unity.Entities.Serialization;

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

                    var poolEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                    var bakedPrefab = GetEntity(poolDef.Prefab, TransformUsageFlags.Dynamic);

                    AddComponent(poolEntity, new ElementPool
                    {
                        ModelType = poolDef.ModelType,
                        PrefabReference = default,
                        PoolSize = poolDef.InitialCount,
                        Prefab = bakedPrefab
                    });

                    // Add buffer to store the actual pooled entities
                    AddBuffer<PooledEntity>(poolEntity);
                }
            }
        }
    }
}
