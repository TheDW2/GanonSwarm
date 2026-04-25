using UnityEngine;

// Attach to the MeleeEnemy GameObject that has the Animator component
public class MeleeEnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private MeleeEnemy meleeEnemy;

    private static readonly int IsWalking = Animator.StringToHash("isWalking");

    void Awake()
    {
        animator = GetComponent<Animator>();
        // If animator is on a child sprite object, use GetComponentInParent
        meleeEnemy = GetComponent<MeleeEnemy>();
        if (meleeEnemy == null)
            meleeEnemy = GetComponentInParent<MeleeEnemy>();
    }

    void Update()
    {
        if (meleeEnemy == null) return;
        animator.SetBool(IsWalking, meleeEnemy.IsMoving);
    }
}