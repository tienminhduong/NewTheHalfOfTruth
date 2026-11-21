using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Tracks in-flight Addressables load operations so systems (e.g. SceneService) can wait
/// until everything a freshly loaded scene requested has finished loading.
/// Released or completed handles are pruned automatically; callers only need to call <see cref="Track"/>.
/// </summary>
public static class AddressableLoadTracker
{
    private static readonly List<AsyncOperationHandle> PendingHandles = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PendingHandles.Clear();
    }

    public static void Track(AsyncOperationHandle handle)
    {
        if (handle.IsValid() && !handle.IsDone)
            PendingHandles.Add(handle);
    }

    public static bool HasPendingLoads
    {
        get
        {
            // A handle is no longer pending once it is done or has been released (invalid).
            PendingHandles.RemoveAll(h => !h.IsValid() || h.IsDone);
            return PendingHandles.Count > 0;
        }
    }

    public static UniTask WaitForPendingLoadsAsync(CancellationToken cancellationToken = default)
    {
        return UniTask.WaitWhile(() => HasPendingLoads, cancellationToken: cancellationToken);
    }
}
