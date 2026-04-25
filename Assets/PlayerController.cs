using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("References")]
    // Drag your sprite child GameObject here — it stays upright and vibrates during dash windup
    public Transform spriteObject;

    [Header("Dash — Windup Times")]
    public float minWindupTime = 2f;   // Minimum hold before dash registers
    public float maxWindupTime = 4f;   // Hold time for maximum dash distance

    [Header("Dash — Distances")]
    public float minDashDistance = 3f; // Distance at minWindupTime
    public float maxDashDistance = 8f; // Distance at maxWindupTime

    [Header("Dash — Feel")]
    public float dashDuration = 0.15f;       // How long the dash travel takes
    public float vibrationStrength = 0.05f;  // How far the sprite shakes during windup
    public float vibrationSpeed = 30f;       // How fast the sprite shakes

    // ─────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;

    private bool isWindingUp = false;
    private float windupTimer = 0f;
    private bool isDashing = false;

    private Vector2 dashDirection;
    private Vector3 spriteOrigin; // local position of sprite child at rest

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (spriteObject != null)
            spriteOrigin = spriteObject.localPosition;
    }

    void Update()
    {
        HandleMovementInput();
        HandleAiming();
        HandleDash();
    }

    void FixedUpdate()
    {
        if (!isDashing)
            rb.linearVelocity = moveInput * moveSpeed;
    }

    // ─────────────────────────────────────────
    //  Movement
    // ─────────────────────────────────────────
    void HandleMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;
    }

    // ─────────────────────────────────────────
    //  Aiming — root rotates, sprite stays upright
    // ─────────────────────────────────────────
    void HandleAiming()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = (mouseWorldPos - transform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        // Keep sprite child upright regardless of root rotation
        if (spriteObject != null && !isWindingUp)
            spriteObject.rotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────
    //  Dash
    // ─────────────────────────────────────────
    void HandleDash()
    {
        if (isDashing) return;

        // Begin windup
        if (Input.GetKeyDown(KeyCode.Space) && !isWindingUp)
        {
            isWindingUp = true;
            windupTimer = 0f;

            // Lock dash direction to mouse direction at moment of press
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            dashDirection = ((Vector2)(mouseWorldPos - transform.position)).normalized;
        }

        // Hold — accumulate windup, cap at maxWindupTime, vibrate sprite
        if (isWindingUp && Input.GetKey(KeyCode.Space))
        {
            windupTimer = Mathf.Min(windupTimer + Time.deltaTime, maxWindupTime);

            // Sprite vibration — oscillate local position
            if (spriteObject != null)
            {
                float vibX = Mathf.Sin(Time.time * vibrationSpeed) * vibrationStrength;
                float vibY = Mathf.Cos(Time.time * vibrationSpeed * 1.3f) * vibrationStrength;
                spriteObject.localPosition = spriteOrigin + new Vector3(vibX, vibY, 0f);
                spriteObject.rotation = Quaternion.identity; // keep upright while vibrating
            }
        }

        // Released — decide what happens
        if (isWindingUp && Input.GetKeyUp(KeyCode.Space))
        {
            isWindingUp = false;

            // Reset sprite position
            if (spriteObject != null)
                spriteObject.localPosition = spriteOrigin;

            if (windupTimer < minWindupTime)
            {
                // Too short — cancel, do nothing
                windupTimer = 0f;
                return;
            }

            // Calculate dash distance based on how long they held
            // Clamp between min and max, then lerp distance
            float t = Mathf.InverseLerp(minWindupTime, maxWindupTime, windupTimer);
            float distance = Mathf.Lerp(minDashDistance, maxDashDistance, t);

            StartCoroutine(PerformDash(dashDirection, distance));
            windupTimer = 0f;
        }
    }

    IEnumerator PerformDash(Vector2 direction, float distance)
    {
        isDashing = true;

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
        isDashing = false;
    }
}