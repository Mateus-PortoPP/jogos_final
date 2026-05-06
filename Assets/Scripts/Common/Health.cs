using System;
using UnityEngine;

namespace TowerDefense.Common
{
    /// <summary>
    /// Componente de HP reutilizável. Implementa IDamageable.
    /// Dispara eventos C# pra que outros scripts (animator, FX, etc.) reajam.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField, Tooltip("Tempo até destruir o GameObject após morrer (pra rodar a animação de morte).")]
        private float destroyDelay = 0.6f;
        [SerializeField, Tooltip("Se true, destroi o GameObject ao morrer.")]
        private bool destroyOnDeath = true;

        public event Action<int, int> Damaged; // current, max
        public event Action Died;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead { get; private set; }

        private void Awake()
        {
            Current = maxHealth;
            IsDead = false;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            // Permite que outro componente (ex: escudo) cancele o dano antes de aplicar
            var blocker = GetComponent<IDamageBlocker>();
            if (blocker != null && blocker.BlocksDamage)
            {
                blocker.OnDamageBlocked(amount);
                return;
            }

            Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(Current, maxHealth);

            if (Current == 0)
            {
                IsDead = true;
                Died?.Invoke();
                if (destroyOnDeath) Destroy(gameObject, destroyDelay);
            }
        }

        /// <summary>Permite restaurar HP (futuro: poções, respawn).</summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Damaged?.Invoke(Current, maxHealth);
        }
    }
}
