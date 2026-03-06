using ElementLogicFail.Scripts.Components.Element;
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
                    var rpcEntity = em.CreateEntity();
                    em.AddComponentData(rpcEntity, new SpawnMinionRpc { ModelType = (UnitModelType)modelTypeIndex });
                    em.AddComponentData(rpcEntity, new SendRpcCommandRequest());
                    break;
                }
            }
        }
    }
}
