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

                var serverWorld = ClientServerBootstrap.ServerWorld;
                var serverNetworkEntity = serverWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestListen));
                serverWorld.EntityManager.SetComponentData(serverNetworkEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });
                // Unity 1.0+ Netcode handles relay via RelayServerData component (defined inside Unity.NetCode or we can use generic endpoints)
                
                var clientWorld = ClientServerBootstrap.ClientWorld;
                var clientNetworkEntity = clientWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestConnect));
                clientWorld.EntityManager.SetComponentData(clientNetworkEntity, new NetworkStreamRequestConnect { Endpoint = NetworkEndpoint.LoopbackIpv4 });

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
                var clientNetworkEntity = clientWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestConnect));
                clientWorld.EntityManager.SetComponentData(clientNetworkEntity, new NetworkStreamRequestConnect { Endpoint = NetworkEndpoint.AnyIpv4 });

                Debug.Log($"[Matchmaking] Joined Game with Code: {joinCode}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Failed to Join Game: {e.Message}");
            }
        }
    }
}
