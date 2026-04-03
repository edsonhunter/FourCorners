using ElementLogicFail.Scripts.Components.Minion;
using ElementLogicFail.Scripts.Components.Request;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ElementLogicFail.Scripts.MonoBridge
{
    public class ClientInputBridge : MonoBehaviour
    {
        public void SendSpawnCommand(int modelTypeIndex)
        {
            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    var em = world.EntityManager;
                    var networkQuery = em.CreateEntityQuery(typeof(NetworkId), typeof(NetworkStreamInGame));
                    if (networkQuery.IsEmpty) break;
                    
                    var targetConnection = networkQuery.GetSingletonEntity();

                    var rpcEntity = em.CreateEntity();
                    em.AddComponentData(rpcEntity, new SpawnMinionRpc { ModelType = (UnitModelType)modelTypeIndex });
                    em.AddComponentData(rpcEntity, new SendRpcCommandRequest { TargetConnection = targetConnection });
                    break;
                }
            }
        }
    }
}
