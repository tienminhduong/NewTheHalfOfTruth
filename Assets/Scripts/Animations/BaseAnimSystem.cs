using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;

public class BaseAnimSystem
{
    private PlayableGraph _playableGraph;
    private AnimationPlayableOutput _playableOutput;
    private AnimationClipPlayable _currentClipPlayable;
    private AnimationClip _activeClip;

    private AsyncOperationHandle<IList<AnimationClip>> _preloadHandle;
    private readonly Dictionary<string, AnimationClip> _loadedClips = new();

    private bool _isInitialized = false;
    public bool IsInitialized => _isInitialized;

    public BaseAnimSystem(Animator animator)
    {
        SetupPlayableGraph(animator);
    }

    public BaseAnimSystem(Animator animator, List<CD.AnimClipReference> animRefs, string defaultAnimKey = null)
    {
        SetupPlayableGraph(animator);
        PreloadAnimationClipsAsync(animRefs, defaultAnimKey).Forget();
    }

    /// <summary>
    /// Preloads animation clips asynchronously using Addressables.
    /// </summary>
    /// <param name="animRefs">List of animation clip references</param>
    /// <param name="defaultAnimKey">The key of the default animation clip to play after loaded</param>
    /// <returns></returns>
    private async UniTask PreloadAnimationClipsAsync(List<CD.AnimClipReference> animRefs, string defaultAnimKey = null)
    {
        _isInitialized = false;

        // Create a list to hold the valid keys for the animation clips
        var keys = new List<object>(animRefs.Count);
        foreach (var animRef in animRefs)
        {
            if (animRef.RuntimeKeyIsValid())
                keys.Add(animRef.RuntimeKey);
            else
                Debug.LogWarning($"Invalid animation reference: {animRef.AssetGUID}");
        }

        // Load the animation clips asynchronously using Addressables
        _preloadHandle = Addressables.LoadAssetsAsync<AnimationClip>(keys, OnClipLoaded, Addressables.MergeMode.Union);
        await _preloadHandle.ToUniTask();

        if (_preloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _isInitialized = true;
            if (!string.IsNullOrEmpty(defaultAnimKey))
                PlayAnimation(defaultAnimKey);
        }
        else
            Debug.LogError("Failed to preload animation clips.");
    }

    private void SetupPlayableGraph(Animator animator)
    {
        _playableGraph = PlayableGraph.Create($"{animator.name} Animation System");
        _playableOutput = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", animator);
        _playableGraph.Play();
    }

    private void OnClipLoaded(AnimationClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("Loaded animation clip is null.");
            return;
        }

        if (_loadedClips.ContainsKey(clip.name))
        {
            Debug.LogWarning($"Animation clip '{clip.name}' is already loaded.");
            return;
        }

        _loadedClips[clip.name] = clip;
    }

    public void PlayAnimation(AnimationClip clip)
    {
        if (clip == _activeClip)
            return;

        _activeClip = clip;

        if (_currentClipPlayable.IsValid())
            _playableGraph.DestroyPlayable(_currentClipPlayable);

        _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
        _playableOutput.SetSourcePlayable(_currentClipPlayable);
    }

    /// <summary>
    /// Plays an animation clip based on the provided animation key. Log error message if clip is not found or loaded
    /// </summary>
    /// <param name="animKey">The key of the animation clip to play, by default is the name of the clip</param>
    public void PlayAnimation(string animKey)
    {
        if (!_loadedClips.TryGetValue(animKey, out var clip))
        {
            Debug.LogError($"Animation clip with key '{animKey}' not found.");
            return;
        }

        PlayAnimation(clip);
    }

    public virtual void Destroy()
    {
        if (_playableGraph.IsValid())
            _playableGraph.Destroy();

        if (_preloadHandle.IsValid())
        {
            Addressables.Release(_preloadHandle);
            _preloadHandle = default;
            _isInitialized = false;
        }
    }
}
