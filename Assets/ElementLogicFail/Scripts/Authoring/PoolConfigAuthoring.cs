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
            public string AddressableKey;
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
                    if (string.IsNullOrEmpty(poolDef.AddressableKey)) continue;

                    var poolEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                    AddComponent(poolEntity, new ElementPool
                    {
                        ModelType = poolDef.ModelType,
                        AddressableKey = poolDef.AddressableKey,
                        PoolSize = poolDef.InitialCount,
                        Prefab = Entity.Null
                    });

                    // Add buffer to store the actual pooled entities
                    AddBuffer<PooledEntity>(poolEntity);
                }
            }
        }
    }
}
