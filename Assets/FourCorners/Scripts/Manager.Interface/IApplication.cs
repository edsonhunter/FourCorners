using FourCorners.Scripts.Services.Interface;

namespace FourCorners.Scripts.Manager.Interface
{
    public interface IApplication
    {
        public T GetService<T>() where T : IService;
        public T GetManager<T>() where T : IManager;
    }
}
