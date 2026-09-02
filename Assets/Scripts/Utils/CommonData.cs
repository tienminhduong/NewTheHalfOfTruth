using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CD
{
    [Serializable]
    public struct LoadSceneData
    {
        public AssetReference SceneToLoad;
    }

    [Serializable]
    public class AnimClipReference : AssetReferenceT<AnimationClip>
    {
        public AnimClipReference(string guid) : base(guid) { }
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