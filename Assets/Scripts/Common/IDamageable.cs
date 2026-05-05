namespace TowerDefense.Common
{
    /// <summary>
    /// Interface implementada por qualquer coisa que possa receber dano.
    /// Hitboxes procuram por IDamageable nos colliders atingidos.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int amount);
        bool IsDead { get; }
    }
}
