using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private float lifetime;
    private Rigidbody2D rb;

    public void Init(Vector2 direction, float speed, float damage, float lifetime)
    {
        this.damage = damage;
        this.lifetime = lifetime;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only damage the player
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && other.CompareTag("Player"))
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}