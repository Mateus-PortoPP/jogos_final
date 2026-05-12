using UnityEngine;
using TowerDefense.Common;
using TowerDefense.Manager;

namespace TowerDefense.Player
{
    /// <summary>
    /// Aplica os upgrades do cavaleiro no início da cena, baseado em
    /// GameManager.PlayerUpgradeLevel. Cada nível adiciona:
    ///   - +damagePerLevel ao dano (PlayerCombat)
    ///   - +hpPerLevel ao HP máximo (Health)
    ///   - +speedPerLevel à velocidade (PlayerController)
    ///
    /// Coloque no GameObject do Player. Roda só uma vez no Start.
    /// </summary>
    public class PlayerUpgradeApplier : MonoBehaviour
    {
        [SerializeField] private int damagePerLevel = 5;
        [SerializeField] private int hpPerLevel = 20;
        [SerializeField] private float speedPerLevel = 0.5f;

        private void Start()
        {
            int level = GameManager.Instance != null ? GameManager.Instance.PlayerUpgradeLevel : 0;
            if (level <= 0) return;

            GetComponent<PlayerCombat>()?.AddDamage(damagePerLevel * level);
            GetComponent<Health>()?.IncreaseMaxHealth(hpPerLevel * level);
            GetComponent<PlayerController>()?.AddSpeed(speedPerLevel * level);

            Debug.Log($"[PlayerUpgradeApplier] Aplicados {level} upgrades: +{damagePerLevel * level} dano, +{hpPerLevel * level} HP, +{speedPerLevel * level} velocidade.");
        }
    }
}
