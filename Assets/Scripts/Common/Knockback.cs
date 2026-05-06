using UnityEngine;

namespace TowerDefense.Common
{
    /// <summary>
    /// Aplica empurrão (knockback) e mantém um estado "stunned" por um tempo curto.
    /// Outros scripts (ex: AI do Goblin) devem checar IsStunned pra suspender
    /// movimento/decisões enquanto o knockback acontece, senão eles sobrescrevem
    /// a velocidade aplicada.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Knockback : MonoBehaviour
    {
        [SerializeField, Tooltip("Duração padrão do estado de stun depois do empurrão.")]
        private float defaultStunDuration = 0.2f;

        public bool IsStunned { get; private set; }

        private Rigidbody2D rb;
        private float stunEndTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void Apply(Vector2 direction, float force, float stunDuration = -1f)
        {
            if (rb == null) return;
            rb.linearVelocity = direction.normalized * force;
            stunEndTime = Time.time + (stunDuration > 0f ? stunDuration : defaultStunDuration);
            IsStunned = true;
        }

        private void Update()
        {
            if (IsStunned && Time.time >= stunEndTime)
                IsStunned = false;
        }
    }
}
