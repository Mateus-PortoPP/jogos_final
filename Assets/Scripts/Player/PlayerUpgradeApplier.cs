using UnityEngine;
using TowerDefense.Common;
using TowerDefense.Manager;

namespace TowerDefense.Player
{
    /// <summary>
    /// Aplica os upgrades do cavaleiro:
    ///   - Cada nível adiciona +damagePerLevel ao dano, +hpPerLevel ao HP máximo,
    ///     +speedPerLevel à velocidade.
    ///   - Quando o jogador compra um novo upgrade durante a cena (via InWaveUpgradePanel),
    ///     aplica o delta IMEDIATAMENTE e cura o HP até o máximo — o upgrade serve
    ///     também como "poção" entre ondas.
    ///   - Quando a cena carrega com upgrades já comprados (level > 0), reaplica
    ///     o total acumulado pra reconstituir o estado do jogador.
    ///
    /// Coloque no GameObject do Player.
    /// </summary>
    public class PlayerUpgradeApplier : MonoBehaviour
    {
        [SerializeField] private int damagePerLevel = 2;
        [SerializeField] private int hpPerLevel = 4;
        [SerializeField] private float speedPerLevel = 0.3f;
        [Tooltip("Se true, cada compra cura o HP atual até o máximo (depois do incremento de hpPerLevel).")]
        [SerializeField] private bool healOnUpgrade = true;

        private int lastAppliedLevel;

        private void Start()
        {
            if (GameManager.Instance == null) return;

            int level = GameManager.Instance.PlayerUpgradeLevel;
            if (level > 0)
            {
                ApplyDelta(level);
                lastAppliedLevel = level;
            }

            GameManager.Instance.OnPlayerUpgradeChanged += HandleUpgradeChanged;
            GameManager.Instance.OnPlayerHealPurchased += HandleHealPurchased;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerUpgradeChanged -= HandleUpgradeChanged;
                GameManager.Instance.OnPlayerHealPurchased -= HandleHealPurchased;
            }
        }

        private void HandleHealPurchased()
        {
            var h = GetComponent<Health>();
            if (h != null) h.Heal(h.Max); // cura total (clampa no máximo)
        }

        private void HandleUpgradeChanged(int newLevel)
        {
            int delta = newLevel - lastAppliedLevel;
            if (delta > 0)
            {
                ApplyDelta(delta);
                if (healOnUpgrade)
                {
                    var h = GetComponent<Health>();
                    if (h != null) h.Heal(h.Max); // clampa no máximo
                }
            }
            lastAppliedLevel = newLevel;
        }

        private void ApplyDelta(int delta)
        {
            if (delta <= 0) return;
            GetComponent<PlayerCombat>()?.AddDamage(damagePerLevel * delta);
            GetComponent<Health>()?.IncreaseMaxHealth(hpPerLevel * delta);
            GetComponent<PlayerController>()?.AddSpeed(speedPerLevel * delta);
            Debug.Log($"[PlayerUpgradeApplier] +{delta} níveis: +{damagePerLevel * delta} dano, +{hpPerLevel * delta} HP, +{speedPerLevel * delta} velocidade.");
        }
    }
}
