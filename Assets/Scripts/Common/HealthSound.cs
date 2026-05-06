using UnityEngine;

namespace TowerDefense.Common
{
    /// <summary>
    /// Toca sons reagindo aos eventos do Health no mesmo GameObject.
    /// Usa AudioSource.PlayClipAtPoint pra continuar tocando mesmo se o GameObject
    /// for destruído (caso da morte).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthSound : MonoBehaviour
    {
        [Tooltip("Som tocado ao tomar dano (HP diminui).")]
        [SerializeField] private AudioClip damageSound;
        [Tooltip("Som tocado ao morrer (HP zera). Opcional.")]
        [SerializeField] private AudioClip deathSound;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private Health health;
        private int lastCurrent;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (health != null) lastCurrent = health.Max;
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(int current, int max)
        {
            // Damaged também dispara em Heal. Só toca o som se a vida realmente caiu.
            if (current < lastCurrent && damageSound != null)
                AudioSource.PlayClipAtPoint(damageSound, transform.position, volume);
            lastCurrent = current;
        }

        private void OnDied()
        {
            if (deathSound != null)
                AudioSource.PlayClipAtPoint(deathSound, transform.position, volume);
        }
    }
}
