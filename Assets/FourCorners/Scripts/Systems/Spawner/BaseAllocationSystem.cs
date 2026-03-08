using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace ElementLogicFail.Scripts.Systems.Spawner
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BaseAllocationSystem : ISystem
    {
        private ComponentLookup<Components.Spawner.PlayerBase> _playerBaseLookup;
        private ComponentLookup<Components.Spawner.Spawner> _spawnerLookup;
        private EntityQuery _unassignedBasesQuery;
        private EntityQuery _newPlayersQuery;
        private EntityQuery _allSpawnersQuery;

        [BurstCompile]
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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _playerBaseLookup.Update(ref state);
            _spawnerLookup.Update(ref state);

            var newPlayers = _newPlayersQuery.ToEntityArray(Allocator.TempJob);
            var unassignedBases = _unassignedBasesQuery.ToEntityArray(Allocator.TempJob);
            var allSpawners = _allSpawnersQuery.ToEntityArray(Allocator.TempJob);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new BaseAllocationJob
            {
                NewPlayers = newPlayers,
                UnassignedBases = unassignedBases,
                AllSpawners = allSpawners,
                NetworkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(true),
                PlayerBaseLookup = _playerBaseLookup,
                SpawnerLookup = _spawnerLookup,
                Ecb = ecb
            };

            job.Run();

            ecb.Playback(state.EntityManager);
            
            ecb.Dispose();
            newPlayers.Dispose();
            unassignedBases.Dispose();
            allSpawners.Dispose();
        }
    }

    [BurstCompile]
    public struct BaseAllocationJob : IJob
    {
        [ReadOnly] public NativeArray<Entity> NewPlayers;
        [ReadOnly] public NativeArray<Entity> UnassignedBases;
        [ReadOnly] public NativeArray<Entity> AllSpawners;

        [ReadOnly] public ComponentLookup<NetworkId> NetworkIdLookup;
        public ComponentLookup<Components.Spawner.PlayerBase> PlayerBaseLookup;
        public ComponentLookup<Components.Spawner.Spawner> SpawnerLookup;

        public EntityCommandBuffer Ecb;

        public void Execute()
        {
            int baseIndex = 0;

            for (int i = 0; i < NewPlayers.Length; i++)
            {
                var playerEntity = NewPlayers[i];
                var networkId = NetworkIdLookup[playerEntity];

                for (; baseIndex < UnassignedBases.Length; baseIndex++)
                {
                    var baseEntity = UnassignedBases[baseIndex];
                    var baseData = PlayerBaseLookup[baseEntity];

                    if (!baseData.IsActive)
                    {
                        baseData.IsActive = true;
                        baseData.NetworkId = networkId.Value;
                        PlayerBaseLookup[baseEntity] = baseData;

                        /*// Use FixedString for Burst-compatible string logging
                        var logMsg = new FixedString128Bytes("[Server] Assigned Base ");
                        logMsg.Append((int)baseData.Team);
                        logMsg.Append(" to Player ");
                        logMsg.Append(networkId.Value);
                        UnityEngine.Debug.Log(logMsg);*/

                        for (int j = 0; j < AllSpawners.Length; j++)
                        {
                            var spawnerEntity = AllSpawners[j];
                            var spawnerData = SpawnerLookup[spawnerEntity];
                            if (spawnerData.Team == baseData.Team)
                            {
                                spawnerData.IsActive = true;
                                spawnerData.NetworkId = networkId.Value;
                                SpawnerLookup[spawnerEntity] = spawnerData;
                            }
                        }

                        Ecb.AddComponent<NetworkStreamInGame>(playerEntity);
                        
                        baseIndex++;
                        break;
                    }
                }
            }
        }
    }
}
