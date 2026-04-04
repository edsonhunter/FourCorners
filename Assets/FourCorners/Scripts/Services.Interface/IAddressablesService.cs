using System.Threading.Tasks;

namespace FourCorners.Scripts.Services.Interface
{
    public interface IAddressablesService : IService
    {
        Task InitializeAsync();
        Task PreloadDependenciesAsync(object key);
        Task<T> LoadAssetAsync<T>(object key);
        Task<T> InstantiateAsync<T>(object key);
        void Release(object asset);
    }
}
