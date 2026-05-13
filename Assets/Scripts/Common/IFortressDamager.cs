namespace TowerDefense.Common
{
    /// <summary>
    /// Inimigos que causam dano à fortaleza ao entrar nela implementam essa
    /// interface pra declarar quanto dano causam. Fortress consulta o valor
    /// via GetComponent — sem isso, usa o valor padrão de damagePerEnemy.
    /// </summary>
    public interface IFortressDamager
    {
        int FortressDamage { get; }
    }
}
