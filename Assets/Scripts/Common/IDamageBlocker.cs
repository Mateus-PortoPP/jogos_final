namespace TowerDefense.Common
{
    /// <summary>
    /// Implementado por componentes que podem cancelar o dano antes que o Health o aplique.
    /// O Health busca um IDamageBlocker no mesmo GameObject e ignora o dano se BlocksDamage = true.
    /// Útil pra escudo, fase de invulnerabilidade, etc.
    /// </summary>
    public interface IDamageBlocker
    {
        bool BlocksDamage { get; }
    }
}
