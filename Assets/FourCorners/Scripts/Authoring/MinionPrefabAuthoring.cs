using System;
using System.Collections.Generic;
using ElementLogicFail.Scripts.Components.Minion;
using Unity.Entities;
using UnityEngine;

namespace ElementLogicFail.Scripts.Authoring
{
    public class MinionPrefabAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct PrefabDefinition
        {
            public GameObject Prefab;
            public int InitialCount;
            public UnitModelType ModelType;
        }

        public List<PrefabDefinition> Prefabs;

        public class PrefabBaker : Baker<MinionPrefabAuthoring>
        {
            public override void Bake(MinionPrefabAuthoring authoring)
            {
                if (authoring.Prefabs == null) return;

                foreach (var def in authoring.Prefabs)
                {
                    if (def.Prefab == null) continue;

                    var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                    var bakedPrefab = GetEntity(def.Prefab, TransformUsageFlags.Dynamic);

                    AddComponent(entity, new MinionPrefabDescriptor
                    {
                        ModelType = def.ModelType,
                        PrefabReference = default,
                        InitialCount = def.InitialCount,
                        Prefab = bakedPrefab
                    });
                }
            }
        }
    }
}
