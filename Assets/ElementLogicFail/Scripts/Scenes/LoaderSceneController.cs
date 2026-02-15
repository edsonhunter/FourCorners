using System.Threading.Tasks;
using ElementLogicFail.Scripts.Manager.Interface;
using ElementLogicFail.Scripts.Scenes.Interface;

namespace ElementLogicFail.Scripts.Scenes
{
    public class LoaderSceneController : BaseScene<LoaderData>
    {
        protected override async Task Loading()
        {
            await Task.Run(async () =>
            {
                await Task.Delay(2);
            });
        }

        protected override void Loaded()
        {
            GetManager<ISceneManager>().LoadScene(new MainMenuData());
        }

        protected override void Unload()
        {
        }
    }
    
    public class LoaderData : ISceneData { }
}