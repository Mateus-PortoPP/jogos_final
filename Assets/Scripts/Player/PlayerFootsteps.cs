using UnityEngine;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// Toca um som de passo em intervalos regulares enquanto o player anda no chão.
    /// - Não toca no ar (a menos que requireGrounded = false).
    /// - Não toca se morto, defendendo, ou parado.
    /// </summary>
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("Som")]
        [SerializeField] private AudioClip stepSound;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Header("Timing")]
        [Tooltip("Tempo entre passos enquanto andando. Ajuste pra casar com a animação.")]
        [SerializeField] private float stepInterval = 0.35f;

        [Header("Condições")]
        [Tooltip("Velocidade horizontal mínima pra contar como 'andando'.")]
        [SerializeField] private float minSpeed = 0.1f;
        [Tooltip("Se true, exige estar no chão pra tocar o passo.")]
        [SerializeField] private bool requireGrounded = true;

        private Rigidbody2D rb;
        private PlayerController controller;
        private Health health;
        private float nextStepTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (stepSound == null || rb == null) return;
            if (health != null && health.IsDead) return;

            bool grounded = !requireGrounded || (controller != null && controller.IsGrounded);
            bool moving = Mathf.Abs(rb.linearVelocity.x) > minSpeed;

            if (grounded && moving)
            {
                if (Time.time >= nextStepTime)
                {
                    AudioSource.PlayClipAtPoint(stepSound, transform.position, volume);
                    nextStepTime = Time.time + stepInterval;
                }
            }
            else
            {
                // Reseta pra que o primeiro passo toque imediatamente ao voltar a andar
                nextStepTime = 0f;
            }
        }
    }
}
