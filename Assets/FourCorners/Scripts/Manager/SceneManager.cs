using FourCorners.Scripts.Manager.Interface;
using FourCorners.Scripts.Scenes;
using FourCorners.Scripts.Scenes.Interface;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FourCorners.Scripts.Manager
{
    public class SceneManager : ISceneManager
    {
        private BaseScene _activeScene;
        private IApplication _application;
        
        public SceneManager(IApplication application)
        {
            _application = application;
        }
        
        public void LoadScene(ISceneData data)
        {
            SetupSceneToLoad();
            if (data == null)
            {
                return;
            }

            UnitySceneManager.LoadSceneAsync(data.GetType().Name.Replace("Data", "Scene"), LoadSceneMode.Single)
                    .completed +=
                async operation =>
                {
                    _activeScene = GetActiveSceneController();
                    _activeScene.Init(_application, data);
                    _activeScene.SetActiveScene(true);
                    await _activeScene.FireLoading();
                    _activeScene.FireLoaded();
                };
        }

        public void LoadOverlayScene(ISceneData data)
        {
            UnitySceneManager.LoadSceneAsync(data.GetType().Name.Replace("Data", "Scene"), LoadSceneMode.Additive)
                    .completed +=
                async operation =>
                {
                    SetLastLoadedSceneActive();
                    var overlay = GetActiveSceneController();
                    overlay.Init(_application, data);
                    _activeScene.SetActiveScene(false);
                    await overlay.FireLoading();
                    overlay.FireLoaded();
                };
        }

        public void UnloadOverlay(IBaseScene overlay)
        {
            overlay.FireUnload();
            UnitySceneManager.UnloadSceneAsync(UnitySceneManager.GetActiveScene()).completed += operation =>
            {
                _activeScene.SetActiveScene(true);
            };
        }
        
        public void StartFirstScene(ISceneData data)
        {
            LoadScene(data);
        }
        
        private void SetupSceneToLoad()
        {
            if (_activeScene != null)
            {
                _activeScene.SetActiveScene(false);
                _activeScene.FireUnload();
            }
        }

        private void SetLastLoadedSceneActive()
        {
            Scene lastLoadedScene = default;
            var lastSceneIndex = UnitySceneManager.sceneCount - 1;

            while (lastSceneIndex >= 0)
            {
                lastLoadedScene = UnitySceneManager.GetSceneAt(lastSceneIndex);
                if (lastLoadedScene.IsValid() && lastLoadedScene.isLoaded)
                {
                    break;
                }

                lastSceneIndex--;
            }

            UnitySceneManager.SetActiveScene(lastLoadedScene);
        }
        
        private BaseScene GetActiveSceneController()
        {
            Scene activeScene = UnitySceneManager.GetActiveScene();
            GameObject[] overlayRootObjects = activeScene.GetRootGameObjects();

            BaseScene baseScene = null;
            foreach (GameObject rootObject in overlayRootObjects)
            {
                if (rootObject.GetComponent<BaseScene>() == null)
                    continue;
                
                baseScene = rootObject.GetComponent<BaseScene>();
            }

            return baseScene;
        }
    }
}
