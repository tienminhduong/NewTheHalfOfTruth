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

    public static bool TryGetOrWaitForService<T>(out T service, Action<T> callback = null) where T : IService
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var foundService))
        {
            service = (T)foundService;
            callback?.Invoke(service);
            return true;
        }
        else
        {
            service = default;
            if (!waitingOnInitCallbacks.ContainsKey(type))
            {
                waitingOnInitCallbacks[type] = null;
            }
            waitingOnInitCallbacks[type] += (s) => callback?.Invoke((T)s);
            return false;
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