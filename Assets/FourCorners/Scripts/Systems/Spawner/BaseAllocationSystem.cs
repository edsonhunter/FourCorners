using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BaseAllocationSystem : ISystem
    {
        private ComponentLookup<Components.Spawner.Spawner> _spawnerLookup;
        private EntityQuery _unassignedBasesQuery;
        private EntityQuery _newPlayersQuery;

        public void OnCreate(ref SystemState state)
        {
            _spawnerLookup = state.GetComponentLookup<Components.Spawner.Spawner>(false);
            
            _unassignedBasesQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Components.Spawner.Spawner>());

            _newPlayersQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>());
            
            state.RequireForUpdate(_newPlayersQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            _spawnerLookup.Update(ref state);

            var newPlayers = _newPlayersQuery.ToEntityArray(Allocator.Temp);
            var unassignedBases = _unassignedBasesQuery.ToEntityArray(Allocator.Temp);

            int baseIndex = 0;

            foreach (var playerEntity in newPlayers)
            {
                var networkId = state.EntityManager.GetComponentData<NetworkId>(playerEntity);
                
                for (; baseIndex < unassignedBases.Length; baseIndex++)
                {
                    var baseEntity = unassignedBases[baseIndex];
                    var spawnerData = _spawnerLookup[baseEntity];
                    
                    if (!spawnerData.IsActive)
                    {
                        spawnerData.IsActive = true;
                        spawnerData.NetworkId = networkId.Value;
                        _spawnerLookup[baseEntity] = spawnerData;
                        
                        UnityEngine.Debug.Log($"[Server] Assigned Base {baseEntity} to Player {networkId.Value}");

                        state.EntityManager.AddComponent<NetworkStreamInGame>(playerEntity);
                        
                        baseIndex++;
                        break;
                    }
                }
            }

            newPlayers.Dispose();
            unassignedBases.Dispose();
        }
    }
}
