using UnityEngine;
using TowerDefense.Common;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Fortaleza/castelo que o jogador defende.
    ///
    /// Implementa-se como um trigger: qualquer inimigo (Tag = "Enemy") que entra no
    /// collider é considerado como tendo "chegado ao castelo" — dá dano ao HP da
    /// fortaleza (via GameManager) e o inimigo é destruído. Também notifica o
    /// WaveManager pra contabilizar a baixa, senão a wave nunca terminaria.
    ///
    /// Coloque este script num GameObject filho do "Castelo" visual, posicionado na
    /// borda esquerda do mapa, com um Box Collider 2D Trigger cobrindo a área onde
    /// um goblin "encosta" no castelo.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Fortress : MonoBehaviour
    {
        [Tooltip("Dano padrão dado ao HP da fortaleza por cada inimigo que NÃO implementa IFortressDamager. Inimigos que implementam (Goblin, GoblinArmadura) usam o valor próprio deles.")]
        [SerializeField] private int damagePerEnemy = 1;

        [Tooltip("Tag dos inimigos que afetam a fortaleza.")]
        [SerializeField] private string enemyTag = "Enemy";

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            // Garante que é trigger — não queremos colisão física com o castelo
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[Fortress] OnTriggerEnter2D — entrou: {other.name}, tag={other.tag}");

            if (!other.CompareTag(enemyTag))
            {
                Debug.Log($"[Fortress] Ignorando {other.name} (tag {other.tag} != {enemyTag}).");
                return;
            }

            // Se o inimigo já está em processo de morte, ignora (evita registrar baixa duplicada)
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsDead)
            {
                Debug.Log($"[Fortress] {other.name} já estava morto — ignorando.");
                return;
            }

            // Cada inimigo declara seu próprio dano via IFortressDamager.
            // Se não implementa, usa o fallback damagePerEnemy.
            var damager = other.GetComponent<IFortressDamager>();
            int dmg = damager != null ? damager.FortressDamage : damagePerEnemy;
            Debug.Log($"[Fortress] {other.name} chegou no castelo! Tirando {dmg} HP.");

            // Dano à fortaleza
            GameManager.Instance?.TakeFortressDamage(dmg);

            // Avisa o WaveManager que um inimigo escapou pro castelo.
            // O WaveManager decrementa o count (wave pode terminar) E marca a wave
            // como "não-limpa" pra anular o bônus de ouro.
            WaveManager.Instance?.RegisterEnemyReachedCastle();

            // Remove o inimigo da cena
            Destroy(other.gameObject);
        }
    }
}
