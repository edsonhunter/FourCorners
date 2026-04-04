using System.Threading.Tasks;
using FourCorners.Scripts.Manager.Interface;
using FourCorners.Scripts.Scenes.Interface;
using FourCorners.Scripts.Services.Interface;

namespace FourCorners.Scripts.Scenes
{
    public class LoaderSceneController : BaseScene<LoaderData>
    {
        protected override async Task Loading()
        {
            var addressablesService = GetService<IAddressablesService>();
            await addressablesService.InitializeAsync();
            
            
            await addressablesService.PreloadDependenciesAsync("Characters");
            await addressablesService.PreloadDependenciesAsync("Buildings");
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
