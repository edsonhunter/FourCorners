using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourCorners.Scripts.Services.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FourCorners.Scripts.Services
{
    public class AddressablesService : IAddressablesService
    {
        private readonly Dictionary<object, AsyncOperationHandle> _handles = new Dictionary<object, AsyncOperationHandle>();

        public async Task InitializeAsync()
        {
            var handle = Addressables.InitializeAsync();
            await handle.Task;
        }

        public async Task PreloadDependenciesAsync(object key)
        {
            bool keyExists = false;
            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(key, typeof(object), out _))
                {
                    keyExists = true;
                    break;
                }
            }

            if (!keyExists)
            {
                Debug.LogWarning($"[AddressablesService] Attempting to preload an Addressables key that does not exist in the catalog: '{key}'. Skipping.");
                return;
            }

            var handle = Addressables.DownloadDependenciesAsync(key, true);
            await handle.Task;
            Addressables.Release(handle);
        }

        public async Task<T> LoadAssetAsync<T>(object key)
        {
            if (_handles.TryGetValue(key, out var existingHandle))
            {
                if (existingHandle.IsDone)
                    return (T)existingHandle.Result;

                await existingHandle.Task;
                return (T)existingHandle.Result;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            _handles[key] = handle;
            
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            Addressables.Release(handle);
            _handles.Remove(key);
            throw new Exception($"Failed to load asset at key: {key}");
        }

        public async Task<T> InstantiateAsync<T>(object key)
        {
            var handle = Addressables.InstantiateAsync(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (typeof(T) == typeof(GameObject))
                {
                    return (T)(object)handle.Result;
                }
                
                var component = handle.Result.GetComponent<T>();
                if (component == null)
                {
                    throw new Exception($"Asset instantiated but component of type {typeof(T)} not found on {key}");
                }
                return component;
            }

            throw new Exception($"Failed to instantiate asset at key: {key}");
        }

        public void Release(object key)
        {
            if (_handles.TryGetValue(key, out var handle))
            {
                Addressables.Release(handle);
                _handles.Remove(key);
            }
        }
    }
}
