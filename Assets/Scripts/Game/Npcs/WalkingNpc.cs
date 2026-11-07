using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;

[RequireComponent(typeof(Animator))]
public class WalkingNpc : MonoBehaviour
{
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [SerializeField] private List<CD.AnimClipReference> animationReferences;
    [SerializeField] private float moveSpeed = 3f;

    private Vector2 movingDirection = Vector2.right;

    private HumanAnimSystem animationSystem;
    private Animator animator;

    void OnValidate()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        var facingDirection = movingDirection.x > 0 ? CD.FaceDir.Right : CD.FaceDir.Left;
        gameObject.EnsureComponent(ref animator, true);
        animationSystem = new HumanAnimSystem(animator, animationReferences, facingDirection, CD.HumanState.Walk);
    }

    void Update()
    {
        transform.Translate(moveSpeed * Time.deltaTime * (Vector3)movingDirection);
        if (movingDirection.x > 0 && transform.position.x >= rightPoint.position.x)
        {
            movingDirection = Vector2.left;
            animationSystem.PlayAnimation(movingDirection);
        }
        else if (movingDirection.x < 0 && transform.position.x <= leftPoint.position.x)
        {
            movingDirection = Vector2.right;
            animationSystem.PlayAnimation(movingDirection);
        }
    }

    void OnDestroy()
    {
        animationSystem?.Destroy();
    }
}
