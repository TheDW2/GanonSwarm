using System.Collections;
using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranges")]
    public float preferredRange = 8f;
    public float tooCloseRange = 4f;
    public float chaseRange = 15f;

    [Header("Casting")]
    public float castTime = 1.5f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 6f;
    public float projectileDamage = 20f;
    public float projectileLifetime = 4f;
    public float spawnOffset = 0.8f;

    [Header("Backpedal")]
    public float backpedalSpeed = 2.5f;
    public float backpedalDistance = 2f;

    [Header("References")]
    // Drag the sprite child GameObject here — it will be counter-rotated to stay upright
    public Transform spriteObject;

    private enum State { Chasing, Casting, Backpedaling }
    private State currentState = State.Chasing;
    private bool isCasting = false;

    protected override void Start()
    {
        base.Start();
    }

    void Update()
    {
        if (isStunned || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Chasing:
                HandleChasing(dist);
                break;
            case State.Casting:
                FacePlayer();
                KeepSpriteUpright();
                break;
            case State.Backpedaling:
                HandleBackpedaling(dist);
                break;
        }
    }

    void HandleChasing(float dist)
    {
        if (dist <= tooCloseRange)
        {
            currentState = State.Backpedaling;
            return;
        }

        if (dist <= preferredRange)
        {
            if (!isCasting)
                StartCoroutine(CastAndShoot());
            return;
        }

        ChasePlayer();
    }

    void HandleBackpedaling(float dist)
    {
        if (dist >= tooCloseRange + backpedalDistance)
        {
            currentState = State.Casting;
            if (!isCasting)
                StartCoroutine(CastAndShoot());
            return;
        }

        Vector2 awayFromPlayer = (transform.position - player.position).normalized;
        transform.position += (Vector3)(awayFromPlayer * backpedalSpeed * Time.deltaTime);
        FacePlayer();
        KeepSpriteUpright();
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
        FacePlayer();
        KeepSpriteUpright();
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void KeepSpriteUpright()
    {
        if (spriteObject != null)
            spriteObject.rotation = Quaternion.identity;
    }

    IEnumerator CastAndShoot()
    {
        isCasting = true;
        currentState = State.Casting;

        float elapsed = 0f;
        while (elapsed < castTime)
        {
            float dist = Vector2.Distance(transform.position, player.position);

            if (dist < tooCloseRange)
            {
                isCasting = false;
                currentState = State.Backpedaling;
                yield break;
            }

            if (dist > chaseRange)
            {
                isCasting = false;
                currentState = State.Chasing;
                yield break;
            }

            elapsed += Time.deltaTime;
            FacePlayer();
            KeepSpriteUpright();
            yield return null;
        }

        if (player != null)
        {
            Vector2 shootDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
            Vector2 spawnPos = (Vector2)transform.position + shootDirection * spawnOffset;
            ShootProjectile(spawnPos, shootDirection);
        }

        isCasting = false;
        currentState = State.Chasing;
    }

    void ShootProjectile(Vector2 spawnPos, Vector2 direction)
    {
        if (projectilePrefab == null) return;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        Projectile projScript = proj.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Init(direction, projectileSpeed, projectileDamage, projectileLifetime);
        }
        else
        {
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = proj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * projectileSpeed;
            Destroy(proj, projectileLifetime);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, tooCloseRange);

        Gizmos.color = new Color(1f, 1f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, preferredRange);

        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}