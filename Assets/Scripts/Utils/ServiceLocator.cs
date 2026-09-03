using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, IService> services = new();
    private static readonly Dictionary<Type, Action<IService>> waitingOnInitCallbacks = new();

    public static void RegisterService<T>(T service) where T : IService
    {
        var type = typeof(T);
        if (!services.ContainsKey(type))
        {
            services[type] = service;
            if (waitingOnInitCallbacks.TryGetValue(type, out var callback))
            {
                callback?.Invoke(service);
                waitingOnInitCallbacks.Remove(type);
            }
        }
        else
        {
            Debug.LogWarning($"Service of type {type} is already registered.");
        }
    }

    public static void TryOrWaitForService<T>(Action<IService> callback = null) where T : IService
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var foundService))
        {
            callback?.Invoke(foundService);
        }
        else
        {
            if (!waitingOnInitCallbacks.ContainsKey(type))
            {
                waitingOnInitCallbacks[type] = null;
            }
            waitingOnInitCallbacks[type] += callback;
        }
    }

    public static void RemoveServiceCallback<T>(Action<IService> callback) where T : IService
    {
        var type = typeof(T);
        if (waitingOnInitCallbacks.TryGetValue(type, out var existingCallback))
        {
            existingCallback -= callback;
            if (existingCallback == null)
                waitingOnInitCallbacks.Remove(type);
            else
                waitingOnInitCallbacks[type] = existingCallback;
        }
    }

    public static T GetService<T>() where T : IService
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
        {
            return (T)service;
        }
        else
        {
            Debug.LogError($"Service of type {type} is not registered.");
            return default;
        }
    }

    public static void UnregisterService<T>(T service) where T : IService
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            services.Remove(type);
        }
        else
        {
            Debug.LogWarning($"Service of type {type} is not registered.");
        }
    }
}

public interface IService
{
    void Awake();
    void OnDestroy();
}