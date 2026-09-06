using CD;
using UnityEngine;

public interface IHuman
{
    Vector3 ThoughIndicatorPosition { get; }
    void RotateTo(FaceDir direction);
    Transform transform { get; }
}