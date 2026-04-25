using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public event Action OnStomp;

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
    public GameObject chargedSlashObject;  // Enabled at windup start, stays on through slash
    public GameObject windupIndicator;

    // ─────────────────────────────────────────
    //  Q — Ground Stomp
    // ─────────────────────────────────────────
    [Header("Ground Stomp (Q)")]
    public float stompDamage = 50f;
    public float stompStunDuration = 1.5f;
    public float stompCooldown = 8f;
    public float stompActiveDuration = 0.2f;
    public GameObject stompObject;

    // ─────────────────────────────────────────
    [Header("Shared")]
    public LayerMask enemyLayer;

    private bool canQuickAttack = true;
    private float quickCooldownTimer = 0f;

    private bool canChargedAttack = true;
    private float chargedCooldownTimer = 0f;
    private bool isWindingUp = false;
    private float windupTimer = 0f;
    private bool slashFired = false;
    private bool mustRelease = false;

    private bool canStomp = true;
    private float stompCooldownTimer = 0f;

    void Awake()
    {
        if (stompObject != null) stompObject.SetActive(false);
        if (chargedSlashObject != null) chargedSlashObject.SetActive(false);
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
        if (quickCooldownTimer > 0f) quickCooldownTimer -= Time.deltaTime;
        else canQuickAttack = true;

        if (chargedCooldownTimer > 0f) chargedCooldownTimer -= Time.deltaTime;
        else canChargedAttack = true;

        if (stompCooldownTimer > 0f) stompCooldownTimer -= Time.deltaTime;
        else canStomp = true;
    }

    void HandleQuickSlash()
    {
        if (Input.GetMouseButtonDown(0) && canQuickAttack)
            StartCoroutine(PerformQuickSlash());
    }

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

                // Cancelled before firing — hide the charged slash visual
                if (chargedSlashObject != null) chargedSlashObject.SetActive(false);
            }
            return;
        }

        if (mustRelease) return;

        // Begin windup — enable charged slash visual immediately
        if (Input.GetMouseButtonDown(1) && !isWindingUp)
        {
            isWindingUp = true;
            windupTimer = 0f;
            slashFired = false;

            if (windupIndicator != null) windupIndicator.SetActive(true);

            // Start animation now at windup
            if (chargedSlashObject != null) chargedSlashObject.SetActive(true);
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

                // Fire the slash — chargedSlashObject already active, coroutine will deactivate it
                StartCoroutine(PerformChargedSlash());
            }
        }
    }

    void HandleStomp()
    {
        if (Input.GetKeyDown(KeyCode.Q) && canStomp)
        {
            OnStomp?.Invoke();
            StartCoroutine(PerformStomp());
        }
    }

    // ─────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────
    IEnumerator PerformQuickSlash()
    {
        canQuickAttack = false;
        quickCooldownTimer = quickCooldown;

        quickSlashObject.SetActive(true);
        yield return null;

        DoHitbox(quickSlashObject, quickDamage);

        yield return new WaitForSeconds(quickDuration);
        quickSlashObject.SetActive(false);
    }

    IEnumerator PerformChargedSlash()
    {
        canChargedAttack = false;
        chargedCooldownTimer = chargedCooldown;

        // chargedSlashObject already active from windup — just do the hitbox now
        yield return null;

        DoHitbox(chargedSlashObject, chargedDamage);

        yield return new WaitForSeconds(chargedDuration);
        chargedSlashObject.SetActive(false);
    }

    IEnumerator PerformStomp()
    {
        canStomp = false;
        stompCooldownTimer = stompCooldown;

        if (stompObject == null) { Debug.LogWarning("No stomp object assigned!"); yield break; }

        stompObject.SetActive(true);
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
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null) damageable.TakeDamage(stompDamage);

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null) enemy.Stun(stompStunDuration);
            }
        }

        yield return new WaitForSeconds(stompActiveDuration);
        stompObject.SetActive(false);
    }

    void DoHitbox(GameObject slashObject, float damage)
    {
        if (slashObject == null) return;

        Collider2D slashCollider = slashObject.GetComponent<Collider2D>();
        if (slashCollider == null) return;

        List<Collider2D> hits = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;
        Physics2D.OverlapCollider(slashCollider, filter, hits);

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null) damageable.TakeDamage(damage);
        }
    }
}