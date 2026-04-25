// IDamageable.cs
// Place this in your project — any entity that can receive damage implements this interface.
// This keeps PlayerAttack decoupled from specific enemy classes.

public interface IDamageable
{
    void TakeDamage(float amount);
}