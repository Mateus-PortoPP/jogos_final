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
        [Tooltip("Dano dado ao HP da fortaleza por cada inimigo que chega.")]
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
            if (!other.CompareTag(enemyTag)) return;

            // Se o inimigo já está em processo de morte, ignora (evita registrar baixa duplicada)
            var dmg = other.GetComponent<IDamageable>();
            if (dmg != null && dmg.IsDead) return;

            // Dano à fortaleza
            GameManager.Instance?.TakeFortressDamage(damagePerEnemy);

            // Conta como inimigo "derrotado" pra wave terminar (chegou ao castelo, mas pra wave isso é equivalente a morto)
            WaveManager.Instance?.RegisterEnemyDefeated();

            // Remove o inimigo da cena
            Destroy(other.gameObject);
        }
    }
}
