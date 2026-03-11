using System.Threading.Tasks;

namespace ElementLogicFail.Scripts.Services.Interface
{
    public interface IMultiplayerService : IService
    {
        Task AuthenticateAsync();
        
        // Direct IP/Port
        Task<bool> HostDirectGameAsync(ushort port);
        Task<bool> JoinDirectGameAsync(string ip, ushort port);

        // Unity Relay
        Task<string> HostRelayGameAsync(int maxPlayers);
        Task<bool> JoinRelayGameAsync(string joinCode);
    }
}
