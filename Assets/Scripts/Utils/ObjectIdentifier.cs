using System;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectIdentifier
{
    private static Dictionary<string, IIdentifiable> _objectDictionary = new();
    private static Dictionary<string, Action<IIdentifiable>> _pendingCallbacks = new();

    public static void RegisterObject(string id, IIdentifiable obj)
    {
        if (!_objectDictionary.ContainsKey(id))
        {
            _objectDictionary.Add(id, obj);
            if (_pendingCallbacks.TryGetValue(id, out var callbacks))
                callbacks?.Invoke(obj);
        }
        else
        {
            Debug.LogWarning($"Object with ID '{id}' is already registered.");
        }
    }

    public static void UnregisterObject(string id)
    {
        if (_objectDictionary.ContainsKey(id))
        {
            _objectDictionary.Remove(id);
        }
        else
        {
            Debug.LogWarning($"Object with ID '{id}' is not registered.");
        }
    }

    public static IIdentifiable GetObjectById(string id)
    {
        if (_objectDictionary.TryGetValue(id, out var obj))
        {
            return obj;
        }
        else
        {
            Debug.LogWarning($"Object with ID '{id}' not found.");
            return null;
        }
    }

    public static bool TryGetObjectById(string id, out IIdentifiable obj)
    {
        return _objectDictionary.TryGetValue(id, out obj);
    }

    public static void TryOrWaitFor(string id, Action<IIdentifiable> callback)
    {
        if (TryGetObjectById(id, out var obj))
            callback?.Invoke(obj);
        else
        {
            if (!_pendingCallbacks.ContainsKey(id))
                _pendingCallbacks[id] = null;
            _pendingCallbacks[id] += callback;
        }
    }

    public static void RemovePendingCallback(string id, Action<IIdentifiable> callback)
    {
        if (_pendingCallbacks.ContainsKey(id))
        {
            _pendingCallbacks[id] -= callback;
            if (_pendingCallbacks[id] == null)
                _pendingCallbacks.Remove(id);
        }
    }
}

public interface IIdentifiable
{
    string Id { get; }
    GameObject gameObject { get; }
}