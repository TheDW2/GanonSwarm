using UnityEngine;

// Attach to the Sprite child GameObject (the one with the Animator)
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;
    private PlayerAttack playerAttack;

    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsWindingUp = Animator.StringToHash("isWindingUp");
    private static readonly int IsDashing = Animator.StringToHash("isDashing");
    private static readonly int IsStopping = Animator.StringToHash("isStopping");

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponentInParent<PlayerController>();
        playerAttack = GetComponentInParent<PlayerAttack>();

        // Only subscribe to stomp — slashes animate themselves on their own GameObjects
        playerAttack.OnStomp += PlayStomp;
    }

    void OnDestroy()
    {
        if (playerAttack != null)
            playerAttack.OnStomp -= PlayStomp;
    }

    void Update()
    {
        // Windup overrides walk
        animator.SetBool(IsWindingUp, playerController.IsWindingUp);

        if (!playerController.IsWindingUp)
            animator.SetBool(IsWalking, playerController.IsMoving);

        // Dash aura trigger
        if (playerController.DashStartedThisFrame)
            animator.SetTrigger(IsDashing);
    }

    void PlayStomp() => animator.SetTrigger(IsStopping);
}