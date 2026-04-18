using System.Threading.Tasks;

namespace FourCorners.Scripts.Scenes.Interface
{
    public interface IBaseScene
    {
        bool IsActiveScene { get; }
        public Task FireLoading();
        public void FireLoaded();
        public void FireLoop();
        public void FireUnload();
    }
}
