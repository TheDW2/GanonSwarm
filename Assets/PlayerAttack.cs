using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Mouse1 — Quick Sword Slash
    // ─────────────────────────────────────────
    [Header("Quick Slash (Mouse1)")]
    public float quickDamage = 25f;
    public float quickDuration = 0.15f;
    public float quickCooldown = 0.4f;
    public GameObject quickSlashObject;

    // ─────────────────────────────────────────
    //  Mouse2 — Charged Vertical Slash
    // ─────────────────────────────────────────
    [Header("Charged Vertical Slash (Mouse2)")]
    public float chargedDamage = 75f;
    public float chargedDuration = 0.25f;
    public float chargedCooldown = 1.5f;
    public float windupTime = 2.0f;
    public GameObject chargedSlashObject;
    public GameObject windupIndicator;

    // ─────────────────────────────────────────
    //  Q — Ground Stomp
    // ─────────────────────────────────────────
    [Header("Ground Stomp (Q)")]
    public float stompDamage = 50f;
    public float stompStunDuration = 1.5f;
    public float stompCooldown = 8f;
    public float stompActiveDuration = 0.2f; // How long the stomp hitbox stays active
    // Assign a child GameObject with a CircleCollider2D (Is Trigger = true)
    // The circle size in the Inspector defines the stomp radius
    public GameObject stompObject;

    // ─────────────────────────────────────────
    [Header("Shared")]
    public LayerMask enemyLayer;

    // Quick slash state
    private bool canQuickAttack = true;
    private float quickCooldownTimer = 0f;

    // Charged slash state
    private bool canChargedAttack = true;
    private float chargedCooldownTimer = 0f;
    private bool isWindingUp = false;
    private float windupTimer = 0f;
    private bool slashFired = false;
    private bool mustRelease = false;

    // Stomp state
    private bool canStomp = true;
    private float stompCooldownTimer = 0f;

    // ─────────────────────────────────────────

    void Awake()
    {
        if (stompObject != null) stompObject.SetActive(false);
    }

    void Update()
    {
        HandleCooldowns();
        HandleQuickSlash();
        HandleChargedSlash();
        HandleStomp();
    }

    void HandleCooldowns()
    {
        if (quickCooldownTimer > 0f)
            quickCooldownTimer -= Time.deltaTime;
        else
            canQuickAttack = true;

        if (chargedCooldownTimer > 0f)
            chargedCooldownTimer -= Time.deltaTime;
        else
            canChargedAttack = true;

        if (stompCooldownTimer > 0f)
            stompCooldownTimer -= Time.deltaTime;
        else
            canStomp = true;
    }

    // ─────────────────────────────────────────
    //  Mouse1 — Quick Slash
    // ─────────────────────────────────────────
    void HandleQuickSlash()
    {
        if (Input.GetMouseButtonDown(0) && canQuickAttack)
            StartCoroutine(PerformSlash(quickSlashObject, quickDamage, quickDuration, quickCooldown, isCharged: false));
    }

    // ─────────────────────────────────────────
    //  Mouse2 — Charged Slash
    // ─────────────────────────────────────────
    void HandleChargedSlash()
    {
        if (!canChargedAttack) return;

        if (Input.GetMouseButtonUp(1))
        {
            if (windupIndicator != null) windupIndicator.SetActive(false);
            mustRelease = false;

            if (isWindingUp)
            {
                isWindingUp = false;
                windupTimer = 0f;
                slashFired = false;
            }
            return;
        }

        if (mustRelease) return;

        if (Input.GetMouseButtonDown(1) && !isWindingUp)
        {
            isWindingUp = true;
            windupTimer = 0f;
            slashFired = false;
            if (windupIndicator != null) windupIndicator.SetActive(true);
        }

        if (isWindingUp && Input.GetMouseButton(1) && !slashFired)
        {
            windupTimer += Time.deltaTime;

            if (windupTimer >= windupTime)
            {
                slashFired = true;
                mustRelease = true;
                isWindingUp = false;
                if (windupIndicator != null) windupIndicator.SetActive(false);
                StartCoroutine(PerformSlash(chargedSlashObject, chargedDamage, chargedDuration, chargedCooldown, isCharged: true));
            }
        }
    }

    // ─────────────────────────────────────────
    //  Q — Ground Stomp
    // ─────────────────────────────────────────
    void HandleStomp()
    {
        if (Input.GetKeyDown(KeyCode.Q) && canStomp)
            StartCoroutine(PerformStomp());
    }

    IEnumerator PerformStomp()
    {
        canStomp = false;
        stompCooldownTimer = stompCooldown;

        if (stompObject == null)
        {
            Debug.LogWarning("No stomp object assigned!");
            yield break;
        }

        stompObject.SetActive(true);

        // Wait a frame for the collider to register
        yield return null;

        Collider2D stompCollider = stompObject.GetComponent<Collider2D>();
        if (stompCollider != null)
        {
            List<Collider2D> hits = new List<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = true;

            Physics2D.OverlapCollider(stompCollider, filter, hits);

            foreach (Collider2D hit in hits)
            {
                // Deal damage
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(stompDamage);

                // Apply stun
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                    enemy.Stun(stompStunDuration);
            }
        }

        yield return new WaitForSeconds(stompActiveDuration);
        stompObject.SetActive(false);
    }

    // ─────────────────────────────────────────
    //  Shared slash coroutine
    // ─────────────────────────────────────────
    IEnumerator PerformSlash(GameObject slashObject, float damage, float duration, float cooldown, bool isCharged)
    {
        if (isCharged) { canChargedAttack = false; chargedCooldownTimer = cooldown; }
        else           { canQuickAttack = false;   quickCooldownTimer = cooldown; }

        if (slashObject == null)
        {
            Debug.LogWarning("No slash object assigned!");
            yield break;
        }

        slashObject.SetActive(true);
        yield return null;

        Collider2D slashCollider = slashObject.GetComponent<Collider2D>();
        if (slashCollider != null)
        {
            List<Collider2D> hits = new List<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = true;

            Physics2D.OverlapCollider(slashCollider, filter, hits);

            foreach (Collider2D hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(damage);
            }
        }

        yield return new WaitForSeconds(duration);
        slashObject.SetActive(false);
    }
}