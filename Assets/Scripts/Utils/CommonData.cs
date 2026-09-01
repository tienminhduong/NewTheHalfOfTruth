using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CD
{
    public struct LoadSceneData
    {
        public AssetReference SceneToLoad;
    }

    [System.Serializable]
    public class AnimClipReference : AssetReferenceT<AnimationClip>
    {
        public AnimClipReference(string guid) : base(guid) { }
    }
}