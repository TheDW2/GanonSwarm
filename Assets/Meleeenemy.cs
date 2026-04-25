using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : Enemy
{
    [Header("Melee Attack")]
    public float slashDamage = 15f;
    public float slashRange = 1.5f;
    public float slashWindup = 0.5f;
    public float slashCooldown = 1.5f;
    public float slashActiveDuration = 0.2f;
    public GameObject slashHitboxObject;

    [Header("Layer")]
    public LayerMask playerLayer;

    private bool isAttacking = false;
    private float cooldownTimer = 0f;

    protected override void Start()
    {
        base.Start();
        if (slashHitboxObject != null)
            slashHitboxObject.SetActive(false);
    }

    void Update()
    {
        if (isStunned || isAttacking) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            ChasePlayer();
            return;
        }

        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer <= slashRange)
            StartCoroutine(PerformSlash());
        else
            ChasePlayer();
    }

    void ChasePlayer()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    IEnumerator PerformSlash()
    {
        isAttacking = true;

        // Windup — stand still, keep facing player
        float windupElapsed = 0f;
        while (windupElapsed < slashWindup)
        {
            windupElapsed += Time.deltaTime;

            if (player != null)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }

        // Fire slash
        if (slashHitboxObject != null)
            slashHitboxObject.SetActive(true);

        yield return null;

        Collider2D hitbox = slashHitboxObject != null ? slashHitboxObject.GetComponent<Collider2D>() : null;
        if (hitbox != null)
        {
            List<Collider2D> hits = new List<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(playerLayer);
            filter.useTriggers = true;

            Physics2D.OverlapCollider(hitbox, filter, hits);

            foreach (Collider2D hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(slashDamage);
            }
        }

        yield return new WaitForSeconds(slashActiveDuration);

        if (slashHitboxObject != null)
            slashHitboxObject.SetActive(false);

        cooldownTimer = slashCooldown;
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, slashRange);
    }
}