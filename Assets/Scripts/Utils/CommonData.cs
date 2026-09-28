using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CD
{
    [Serializable]
    public struct LoadSceneData
    {
        public AssetReference SceneToLoad;
        public FaceDir PlayerFacingDirection;
        public string StartPointKey;
    }

    [Serializable]
    public class AnimClipReference : AssetReferenceT<AnimationClip>
    {
        public AnimClipReference(string guid) : base(guid) { }
    }

    [Serializable]
    public struct CutsceneData
    {
        public string sceneTriggerAddress;
        public AssetReference cutsceneAsset;
    }

    public enum FaceDir
    {
        Left,
        Right,
        Up,
        Down
    }

    public enum HumanState
    {
        Idle,
        Walk
    }
}