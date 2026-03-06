using System.Threading.Tasks;

namespace ElementLogicFail.Scripts.Services.Interface
{
    public interface IMultiplayerService : IService
    {
        Task AuthenticateAsync();
        Task<string> HostGameAsync(int maxPlayers);
        Task JoinGameAsync(string joinCode);
    }
}
