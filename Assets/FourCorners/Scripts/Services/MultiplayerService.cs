using System;
using System.Threading.Tasks;
using ElementLogicFail.Scripts.Services.Interface;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Entities;

namespace ElementLogicFail.Scripts.Services
{
    public class MultiplayerService : IMultiplayerService
    {
        public void Init() { }

        public async Task AuthenticateAsync()
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Matchmaking] Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
        }

        public async Task<string> HostGameAsync(int maxPlayers)
        {
            try
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new System.Collections.Generic.Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                    }
                };

                await LobbyService.Instance.CreateLobbyAsync("FourCornersLobby", maxPlayers, options);
                Debug.Log($"[Matchmaking] Lobby created. Join Code: {joinCode}");

                var relayServerData = new RelayServerData(allocation, "dtls");
                var relayClientData = new RelayServerData();
                var driverStore = new NetworkDriverStore();
                var relayConstructor = new RelayDriverConstructor(relayServerData, relayClientData, true);

                var serverWorld = ClientServerBootstrap.ServerWorld;
                var serverNetDebug = serverWorld.EntityManager.CreateEntityQuery(typeof(NetDebug)).GetSingleton<NetDebug>();
                relayConstructor.CreateServerDriver(serverWorld, ref driverStore, serverNetDebug);
                
                var serverDriverQuery = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
                serverDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.ResetDriverStore(serverWorld.Unmanaged, ref driverStore);

                var serverListenEntity = serverWorld.EntityManager.CreateEntity();
                serverWorld.EntityManager.AddComponentData(serverListenEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

                var clientWorld = ClientServerBootstrap.ClientWorld;
                var clientNetDebug = clientWorld.EntityManager.CreateEntityQuery(typeof(NetDebug)).GetSingleton<NetDebug>();
                var clientDriverStore = new NetworkDriverStore();
                relayConstructor.CreateClientDriver(clientWorld, ref clientDriverStore, clientNetDebug);

                var clientDriverQuery = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
                clientDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.ResetDriverStore(clientWorld.Unmanaged, ref clientDriverStore);

                var clientConnectEntity = clientWorld.EntityManager.CreateEntity();
                clientWorld.EntityManager.AddComponentData(clientConnectEntity, new NetworkStreamRequestConnect { Endpoint = NetworkEndpoint.LoopbackIpv4 });

                return joinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Failed to Host Game: {e.Message}");
                return string.Empty;
            }
        }

        public async Task JoinGameAsync(string joinCode)
        {
            try
            {
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                var relayServerData = new RelayServerData(joinAllocation, "dtls");

                // Assuming we want to discard ServerWorld in joiners
                if (ClientServerBootstrap.ServerWorld != null && ClientServerBootstrap.ServerWorld.IsCreated)
                {
                    ClientServerBootstrap.ServerWorld.Dispose();
                }

                var clientWorld = ClientServerBootstrap.ClientWorld;
                var clientNetDebug = clientWorld.EntityManager.CreateEntityQuery(typeof(NetDebug)).GetSingleton<NetDebug>();
                
                var relayConstructor = new RelayDriverConstructor(new RelayServerData(), relayServerData, false);
                var clientDriverStore = new NetworkDriverStore();
                relayConstructor.CreateClientDriver(clientWorld, ref clientDriverStore, clientNetDebug);
                
                var clientDriverQuery = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
                clientDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.ResetDriverStore(clientWorld.Unmanaged, ref clientDriverStore);

                var clientConnectEntity = clientWorld.EntityManager.CreateEntity();
                clientWorld.EntityManager.AddComponentData(clientConnectEntity, new NetworkStreamRequestConnect { Endpoint = NetworkEndpoint.AnyIpv4 });

                Debug.Log($"[Matchmaking] Joined Game with Code: {joinCode}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Failed to Join Game: {e.Message}");
            }
        }
    }
}
