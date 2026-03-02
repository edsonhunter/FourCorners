using ElementLogicFail.Scripts.Manager;
using ElementLogicFail.Scripts.Manager.Camera;
using ElementLogicFail.Scripts.Manager.Interface;
using ElementLogicFail.Scripts.Manager.Interface.Camera;
using ElementLogicFail.Scripts.Scenes;
using ElementLogicFail.Scripts.Services;
using ElementLogicFail.Scripts.Services.Interface;
using ElementLogicFail.Scripts.Utils.Threadpool;
using UnityEngine;

namespace ElementLogicFail.Scripts.Bootstrapper
{
    public class Bootstrapper : MonoBehaviour
    {
        private ApplicationManager _applicationManager;
        
        private void Start()
        {
            //Create applicationPrefab
            //Initialize applicationPrefab
            SetupApplication();
            SetupThreadPool();
            SetupServices();
            SetupManagers();
            StartGame();
        }

        private void SetupApplication()
        {
            _applicationManager = new GameObject().AddComponent<ApplicationManager>();
        }

        private void SetupThreadPool()
        {
            var threadObject = new GameObject().AddComponent<ThreadPoolController>();
            threadObject.transform.SetParent(_applicationManager.transform);
        }

        private void SetupServices()
        {
            _applicationManager.RegisterServices<ISystemBridgeService>(new SystemBridgeService());
            _applicationManager.RegisterServices<IAddressablesService>(new AddressablesService());
        }

        private void SetupManagers()
        {
            //Register all managers
            
            _applicationManager.RegisterManager<ISceneManager>(new SceneManager(_applicationManager));
            _applicationManager.RegisterManager<ICameraManager>(new CameraManager());
        }

        private void StartGame()
        {
            _applicationManager.StartGame();
            _applicationManager.GetManager<ISceneManager>().LoadScene(new LoaderData());
        }
    }
}
