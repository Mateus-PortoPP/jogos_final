using UnityEngine;
using TowerDefense.Common;
using TowerDefense.Manager;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// Aumenta o HP do inimigo conforme a noite atual (GameManager.CurrentNight).
    /// Bônus = hpPerNight * (noite - 1). Aplicado no spawn, sem disparar evento
    /// de dano (não causa flash branco falso). Vale pra qualquer inimigo com Health.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyNightScaling : MonoBehaviour
    {
        [Tooltip("HP extra por noite acima da noite 1.")]
        [SerializeField] private int hpPerNight = 2;

        private void Start()
        {
            if (hpPerNight <= 0) return;
            int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1;
            int bonus = hpPerNight * (night - 1);
            if (bonus > 0) GetComponent<Health>().GrowMaxHealthSilent(bonus);
        }
    }
}
