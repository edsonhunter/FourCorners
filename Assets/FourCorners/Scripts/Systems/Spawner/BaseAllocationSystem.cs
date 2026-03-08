using ElementLogicFail.Scripts.Systems.Connection;
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
        private EntityQuery _goInGameRequestsQuery;
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

            _goInGameRequestsQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest, ReceiveRpcCommandRequest>());
            
            state.RequireForUpdate(_goInGameRequestsQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _playerBaseLookup.Update(ref state);
            _spawnerLookup.Update(ref state);

            var newRequests = _goInGameRequestsQuery.ToEntityArray(Allocator.TempJob);
            var unassignedBases = _unassignedBasesQuery.ToEntityArray(Allocator.TempJob);
            var allSpawners = _allSpawnersQuery.ToEntityArray(Allocator.TempJob);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new BaseAllocationJob
            {
                NewRequests = newRequests,
                UnassignedBases = unassignedBases,
                AllSpawners = allSpawners,
                RpcReceiveLookup = SystemAPI.GetComponentLookup<ReceiveRpcCommandRequest>(true),
                NetworkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(true),
                PlayerBaseLookup = _playerBaseLookup,
                SpawnerLookup = _spawnerLookup,
                Ecb = ecb
            };

            job.Schedule(state.Dependency).Complete();

            ecb.Playback(state.EntityManager);
            
            ecb.Dispose();
            newRequests.Dispose();
            unassignedBases.Dispose();
            allSpawners.Dispose();
        }
    }

    [BurstCompile]
    public struct BaseAllocationJob : IJob
    {
        [ReadOnly] public NativeArray<Entity> NewRequests;
        [ReadOnly] public NativeArray<Entity> UnassignedBases;
        [ReadOnly] public NativeArray<Entity> AllSpawners;

        [ReadOnly] public ComponentLookup<ReceiveRpcCommandRequest> RpcReceiveLookup;
        [ReadOnly] public ComponentLookup<NetworkId> NetworkIdLookup;
        public ComponentLookup<Components.Spawner.PlayerBase> PlayerBaseLookup;
        public ComponentLookup<Components.Spawner.Spawner> SpawnerLookup;

        public EntityCommandBuffer Ecb;

        public void Execute()
        {
            int baseIndex = 0;

            for (int i = 0; i < NewRequests.Length; i++)
            {
                var requestEntity = NewRequests[i];
                var sourceConnection = RpcReceiveLookup[requestEntity].SourceConnection;

                if (!NetworkIdLookup.HasComponent(sourceConnection))
                {
                    Ecb.DestroyEntity(requestEntity);
                    continue;
                }

                var networkId = NetworkIdLookup[sourceConnection].Value;

                for (; baseIndex < UnassignedBases.Length; baseIndex++)
                {
                    var baseEntity = UnassignedBases[baseIndex];
                    var baseData = PlayerBaseLookup[baseEntity];

                    if (!baseData.IsActive)
                    {
                        baseData.IsActive = true;
                        baseData.NetworkId = networkId;
                        PlayerBaseLookup[baseEntity] = baseData;

                        for (int j = 0; j < AllSpawners.Length; j++)
                        {
                            var spawnerEntity = AllSpawners[j];
                            var spawnerData = SpawnerLookup[spawnerEntity];
                            if (spawnerData.Team == baseData.Team)
                            {
                                spawnerData.IsActive = true;
                                spawnerData.NetworkId = networkId;
                                SpawnerLookup[spawnerEntity] = spawnerData;
                            }
                        }

                        // Mark the connection as In-Game so Netcode starts ghost synchronization
                        Ecb.AddComponent<NetworkStreamInGame>(sourceConnection);
                        
                        baseIndex++;
                        break;
                    }
                }
                
                // Always destroy the handled RPC
                Ecb.DestroyEntity(requestEntity);
            }
        }
    }
}
