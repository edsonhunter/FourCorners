using System.Threading.Tasks;

namespace ElementLogicFail.Scripts.Services.Interface
{
    public interface IAddressablesService : IService
    {
        Task InitializeAsync();
        Task<T> LoadAssetAsync<T>(object key);
        Task<T> InstantiateAsync<T>(object key);
        void Release(object asset);
    }
}
