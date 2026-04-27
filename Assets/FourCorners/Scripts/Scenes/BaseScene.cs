using System.Threading.Tasks;
using FourCorners.Scripts.Manager.Interface;
using FourCorners.Scripts.Scenes.Interface;
using FourCorners.Scripts.Services.Interface;
using FourCorners.Scripts.Utils;
using UnityEngine;

namespace FourCorners.Scripts.Scenes
{
    public abstract class BaseScene<T> : BaseScene where T : class, ISceneData
    {
        public new T SceneData => (T) base.SceneData;
    }
    
    public abstract class BaseScene : MonoBehaviour, IBaseScene
    {
        public bool IsActiveScene { get; private set; }
        internal ISceneData SceneData;
        internal IApplication Application;
        
        protected virtual async Task Loading() { await Task.Yield(); }
        protected virtual void Loaded() { }
        protected virtual void Loop() { }
        protected virtual void Unload() { }

        #if UNITY_EDITOR
        protected BaseScene()
        {
            this.AssertForbiddenMethods("Start", "Awake", "Update");
        }
        #endif
        
        public async Task FireLoading()
        {
            await Loading();
        }
        
        public void FireLoaded()
        {
            Loaded();
        }

        public void FireLoop()
        {
            Loop();
        }

        public void FireUnload()
        {
            Unload();
        }

        public void Init(IApplication application, ISceneData data)
        {
            Application = application;
            SceneData = data;
        }
        
        public void SetActiveScene(bool activeScene)
        {
            IsActiveScene = activeScene;
        }

        protected T GetService<T>() where T : IService
        {
            return Application.GetService<T>();
        }

        protected T GetManager<T>() where T : IManager
        {
            return Application.GetManager<T>();
        }
    }
}
