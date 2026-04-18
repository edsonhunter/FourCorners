using FourCorners.Scripts.Scenes.Interface;
using FourCorners.Scripts.Services.Interface;

namespace FourCorners.Scripts.Manager.Interface
{
    public interface ISceneManager : IManager
    {
        void LoadScene(ISceneData data);
        void LoadOverlayScene(ISceneData data);
        void UnloadOverlay(IBaseScene overlay);
    }
}
