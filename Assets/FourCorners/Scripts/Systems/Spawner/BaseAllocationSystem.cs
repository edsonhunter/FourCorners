using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BaseAllocationSystem : ISystem
    {
        private ComponentLookup<Components.Spawner.PlayerBase> _playerBaseLookup;
        private ComponentLookup<Components.Spawner.Spawner> _spawnerLookup;
        private EntityQuery _unassignedBasesQuery;
        private EntityQuery _newPlayersQuery;
        private EntityQuery _allSpawnersQuery;

        public void OnCreate(ref SystemState state)
        {
            _playerBaseLookup = state.GetComponentLookup<Components.Spawner.PlayerBase>(false);
            _spawnerLookup = state.GetComponentLookup<Components.Spawner.Spawner>(false);
            
            _unassignedBasesQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Components.Spawner.PlayerBase>());

            _allSpawnersQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Components.Spawner.Spawner>());

            _newPlayersQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>());
            
            state.RequireForUpdate(_newPlayersQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            _playerBaseLookup.Update(ref state);
            _spawnerLookup.Update(ref state);

            var newPlayers = _newPlayersQuery.ToEntityArray(Allocator.Temp);
            var unassignedBases = _unassignedBasesQuery.ToEntityArray(Allocator.Temp);
            var allSpawners = _allSpawnersQuery.ToEntityArray(Allocator.Temp);

            int baseIndex = 0;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var playerEntity in newPlayers)
            {
                var networkId = state.EntityManager.GetComponentData<NetworkId>(playerEntity);
                
                for (; baseIndex < unassignedBases.Length; baseIndex++)
                {
                    var baseEntity = unassignedBases[baseIndex];
                    var baseData = _playerBaseLookup[baseEntity];
                    
                    if (!baseData.IsActive)
                    {
                        baseData.IsActive = true;
                        baseData.NetworkId = networkId.Value;
                        _playerBaseLookup[baseEntity] = baseData;
                        
                        UnityEngine.Debug.Log($"[Server] Assigned Base {baseData.Team} to Player {networkId.Value}");

                        foreach (var spawnerEntity in allSpawners)
                        {
                            var spawnerData = _spawnerLookup[spawnerEntity];
                            if (spawnerData.Team == baseData.Team)
                            {
                                spawnerData.IsActive = true;
                                spawnerData.NetworkId = networkId.Value;
                                _spawnerLookup[spawnerEntity] = spawnerData;
                            }
                        }

                        ecb.AddComponent<NetworkStreamInGame>(playerEntity);
                        
                        baseIndex++;
                        break;
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            newPlayers.Dispose();
            unassignedBases.Dispose();
            allSpawners.Dispose();
        }
    }
}
