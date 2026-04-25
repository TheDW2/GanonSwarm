using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("References")]
    public Transform spriteObject;

    [Header("Dash — Windup Times")]
    public float minWindupTime = 2f;
    public float maxWindupTime = 4f;

    [Header("Dash — Distances")]
    public float minDashDistance = 3f;
    public float maxDashDistance = 8f;

    [Header("Dash — Feel")]
    public float dashDuration = 0.15f;
    public float vibrationStrength = 0.05f;
    public float vibrationSpeed = 30f;

    [Header("Dash — Damage")]
    public float dashDamage = 40f;
    public LayerMask enemyLayer;
    public GameObject dashHitboxObject;

    // ── Properties read by PlayerAnimator ──
    public bool IsMoving { get; private set; }
    public bool IsWindingUp { get; private set; }
    public bool DashStartedThisFrame { get; private set; }

    // ─────────────────────────────────────────
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;

    private bool isWindingUp = false;
    private float windupTimer = 0f;
    private bool isDashing = false;
    private bool dashFired = false;
    private bool mustRelease = false;

    private Vector3 spriteOrigin;
    private HashSet<Collider2D> dashedThrough = new HashSet<Collider2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (spriteObject != null)
            spriteOrigin = spriteObject.localPosition;

        if (dashHitboxObject != null)
            dashHitboxObject.SetActive(false);
    }

    void Update()
    {
        DashStartedThisFrame = false;

        HandleMovementInput();
        HandleAiming();
        HandleDash();

        if (isDashing)
            CheckDashHits();

        // Update animation properties
        IsMoving = moveInput.magnitude > 0f && !isDashing;
        IsWindingUp = isWindingUp;
    }

    void FixedUpdate()
    {
        if (!isDashing)
            rb.linearVelocity = moveInput * moveSpeed;
    }

    void HandleMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;
    }

    void HandleAiming()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = (mouseWorldPos - transform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        if (spriteObject != null && !isWindingUp)
            spriteObject.rotation = Quaternion.identity;
    }

    void HandleDash()
    {
        if (isDashing) return;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            mustRelease = false;

            if (isWindingUp)
            {
                isWindingUp = false;

                if (spriteObject != null)
                    spriteObject.localPosition = spriteOrigin;

                if (windupTimer >= minWindupTime && !dashFired)
                    FireDash(windupTimer);

                windupTimer = 0f;
                dashFired = false;
            }
            return;
        }

        if (mustRelease) return;

        if (Input.GetKeyDown(KeyCode.Space) && !isWindingUp)
        {
            isWindingUp = true;
            windupTimer = 0f;
            dashFired = false;
        }

        if (isWindingUp && Input.GetKey(KeyCode.Space) && !dashFired)
        {
            windupTimer += Time.deltaTime;

            if (spriteObject != null)
            {
                float vibX = Mathf.Sin(Time.time * vibrationSpeed) * vibrationStrength;
                float vibY = Mathf.Cos(Time.time * vibrationSpeed * 1.3f) * vibrationStrength;
                spriteObject.localPosition = spriteOrigin + new Vector3(vibX, vibY, 0f);
                spriteObject.rotation = Quaternion.identity;
            }

            if (windupTimer >= maxWindupTime)
            {
                windupTimer = maxWindupTime;
                dashFired = true;
                isWindingUp = false;
                mustRelease = true;

                if (spriteObject != null)
                    spriteObject.localPosition = spriteOrigin;

                FireDash(maxWindupTime);
            }
        }
    }

    void FireDash(float heldTime)
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2 toMouse = new Vector2(
            mouseWorldPos.x - transform.position.x,
            mouseWorldPos.y - transform.position.y
        );
        Vector2 direction = toMouse.normalized;

        float t = Mathf.InverseLerp(minWindupTime, maxWindupTime, heldTime);
        float distance = Mathf.Lerp(minDashDistance, maxDashDistance, t);

        DashStartedThisFrame = true;
        StartCoroutine(PerformDash(direction, distance));
    }

    IEnumerator PerformDash(Vector2 direction, float distance)
    {
        isDashing = true;
        dashedThrough.Clear();

        if (dashHitboxObject != null)
            dashHitboxObject.SetActive(true);

        float elapsed = 0f;
        Vector2 startPos = rb.position;
        Vector2 targetPos = startPos + direction * distance;

        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dashDuration);
            rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPos);
        rb.linearVelocity = Vector2.zero;

        if (dashHitboxObject != null)
            dashHitboxObject.SetActive(false);

        dashedThrough.Clear();
        isDashing = false;
    }

    void CheckDashHits()
    {
        if (dashHitboxObject == null) return;

        Collider2D hitbox = dashHitboxObject.GetComponent<Collider2D>();
        if (hitbox == null) return;

        List<Collider2D> hits = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;

        Physics2D.OverlapCollider(hitbox, filter, hits);

        foreach (Collider2D hit in hits)
        {
            if (dashedThrough.Contains(hit)) continue;
            dashedThrough.Add(hit);

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(dashDamage);
        }
    }
}