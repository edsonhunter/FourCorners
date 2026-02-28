using System.Threading.Tasks;
using ElementLogicFail.Scripts.Manager.Interface;
using ElementLogicFail.Scripts.Scenes.Interface;
using ElementLogicFail.Scripts.Services.Interface;

namespace ElementLogicFail.Scripts.Scenes
{
    public class LoaderSceneController : BaseScene<LoaderData>
    {
        protected override async Task Loading()
        {
            var addressablesService = GetService<IAddressablesService>();
            await addressablesService.InitializeAsync();
            
            // Preload core groups to avoid stuttering later
            await addressablesService.PreloadDependenciesAsync("Characters");
            await addressablesService.PreloadDependenciesAsync("Buildings");
            await addressablesService.PreloadDependenciesAsync("Particles");
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