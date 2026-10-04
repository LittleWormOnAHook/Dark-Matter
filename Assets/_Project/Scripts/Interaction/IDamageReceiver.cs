namespace Project.Interaction
{
    /// <summary>Rich damage contract for combat systems. Legacy callers keep using <see cref="IDamageable"/>.</summary>
    public interface IDamageReceiver
    {
        void ReceiveDamage(in Project.Combat.DamageInfo info);
    }
}
