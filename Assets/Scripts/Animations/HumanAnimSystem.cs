using System.Collections.Generic;
using CD;
using UnityEngine;

public class HumanAnimSystem : BaseAnimSystem
{
    private FaceDir _curDir;
    private HumanState _curState;

    public HumanAnimSystem(
        Animator animator,
        List<AnimClipReference> animRefs,
        FaceDir initialDir = FaceDir.Down,
        HumanState initialState = HumanState.Idle
    ) : base(animator, animRefs, $"{initialState}{initialDir}")
    {
        _curDir = initialDir;
        _curState = initialState;
    }

    public void PlayAnimation(FaceDir direction, HumanState state)
    {
        _curDir = direction;
        _curState = state;

        string clipName = $"{state}{direction}";
        PlayAnimation(clipName);
    }

    public void PlayAnimation(Vector2 dir)
    {
        if (!IsInitialized)
            return;

        if (dir == Vector2.zero)
            _curState = HumanState.Idle;
        else
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                _curDir = dir.x > 0 ? FaceDir.Right : FaceDir.Left;
            else
                _curDir = dir.y > 0 ? FaceDir.Up : FaceDir.Down;

            _curState = HumanState.Walk;
        }

        PlayAnimation(_curDir, _curState);
    }
}