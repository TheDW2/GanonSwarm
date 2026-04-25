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
    private bool slashFired = false;   // true once the slash fires during this hold
    private bool mustRelease = false;  // player must release before charging again

    // ─────────────────────────────────────────

    void Update()
    {
        HandleCooldowns();
        HandleQuickSlash();
        HandleChargedSlash();
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
    }

    // ─────────────────────────────────────────
    //  Mouse1
    // ─────────────────────────────────────────
    void HandleQuickSlash()
    {
        if (Input.GetMouseButtonDown(0) && canQuickAttack)
            StartCoroutine(PerformSlash(quickSlashObject, quickDamage, quickDuration, quickCooldown, isCharged: false));
    }

    // ─────────────────────────────────────────
    //  Mouse2
    // ─────────────────────────────────────────
    void HandleChargedSlash()
    {
        // Player released mouse2 — reset so they can charge again
        if (Input.GetMouseButtonUp(1))
        {
            if (windupIndicator != null) windupIndicator.SetActive(false);
            isWindingUp = false;
            windupTimer = 0f;
            mustRelease = false;
            slashFired = false;
            return;
        }

        // Don't start a new charge if we're waiting for a release
        if (mustRelease) return;
        if (!canChargedAttack) return;

        // Begin windup on press
        if (Input.GetMouseButtonDown(1) && !isWindingUp)
        {
            isWindingUp = true;
            windupTimer = 0f;
            slashFired = false;
            if (windupIndicator != null) windupIndicator.SetActive(true);
        }

        // Hold — accumulate time and fire as soon as windup completes
        if (isWindingUp && Input.GetMouseButton(1) && !slashFired)
        {
            windupTimer += Time.deltaTime;

            if (windupTimer >= windupTime)
            {
                // Windup complete — fire immediately, don't wait for release
                slashFired = true;
                mustRelease = true; // require release before next charge
                isWindingUp = false;

                if (windupIndicator != null) windupIndicator.SetActive(false);

                StartCoroutine(PerformSlash(chargedSlashObject, chargedDamage, chargedDuration, chargedCooldown, isCharged: true));
            }
        }
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

        // Wait one frame so the collider registers
        yield return null;

        Collider2D slashCollider = slashObject.GetComponent<Collider2D>();
        if (slashCollider != null)
        {
            List<Collider2D> hits = new List<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = false;

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