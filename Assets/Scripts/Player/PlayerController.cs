using UnityEngine;
using UnityEngine.InputSystem;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// Movimento do Guardião (side-scroller):
    /// - A/D move horizontalmente
    /// - Espaço pula (só do chão)
    /// - Flip horizontal acompanha direção
    ///
    /// Parâmetros do Animator que este script seta (todos opcionais — só seta se o
    /// parâmetro existir no AnimatorController, sem warning):
    ///   - Speed         (float)  — magnitude da velocidade horizontal
    ///   - Grounded      (bool)   — true se está no chão
    ///   - VerticalSpeed (float)  — velocidade vertical (pra Jump/Fall)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Pulo")]
        [SerializeField] private float jumpForce = 7f;
        [Tooltip("Quanto abaixo do collider o raycast sonda o chão.")]
        [SerializeField] private float groundCheckDistance = 0.1f;

        [Header("Visual")]
        [Tooltip("Sprite olha pra direita por padrão? (deixe marcado pro knight)")]
        [SerializeField] private bool spriteFacesRight = true;

        [Header("Animator (nomes dos parâmetros)")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string groundedParam = "Grounded";
        [SerializeField] private string verticalSpeedParam = "VerticalSpeed";
        [SerializeField] private string deathTrigger = "Death";

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Collider2D bodyCollider;
        private Animator animator;
        private AnimatorParamCache animParams;
        private Health health;

        private Vector2 moveInput;
        private bool jumpQueued;
        private bool isDead;

        public bool IsGrounded { get; private set; }
        public bool FacingRight { get; private set; } = true;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            bodyCollider = GetComponent<Collider2D>();
            animator = GetComponent<Animator>();
            animParams = new AnimatorParamCache(animator);
            health = GetComponent<Health>();
            if (health != null) health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        private void OnDied()
        {
            isDead = true;
            // Para o movimento e dispara a animação de morte
            rb.linearVelocity = Vector2.zero;
            moveInput = Vector2.zero;
            jumpQueued = false;
            animParams.SetTrigger(deathTrigger);
            animParams.SetFloat(speedParam, 0f);

            // Desativa o combate enquanto está morto
            var combat = GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;
        }

        private void FixedUpdate()
        {
            // Morto: ignora input mas deixa a gravidade agir
            if (isDead)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                animParams.SetFloat(speedParam, 0f);
                return;
            }

            float vx = moveInput.x * moveSpeed;
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

            IsGrounded = CheckGrounded();

            if (jumpQueued)
            {
                jumpQueued = false;
                if (IsGrounded)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                }
            }

            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                FacingRight = moveInput.x > 0f;
                sr.flipX = spriteFacesRight ? !FacingRight : FacingRight;
            }

            // Anima — todos opcionais, só seta se existir
            animParams.SetFloat(speedParam, Mathf.Abs(vx));
            animParams.SetBool(groundedParam, IsGrounded);
            animParams.SetFloat(verticalSpeedParam, rb.linearVelocity.y);
        }

        private bool CheckGrounded()
        {
            if (bodyCollider == null) return true;
            Vector2 origin = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y - 0.02f);
            var hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance);
            return hit.collider != null;
        }

        // Callbacks do PlayerInput em modo Send Messages
        public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
        public void OnJump(InputValue value) { if (value.isPressed) jumpQueued = true; }

        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y - 0.02f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);
        }
    }
}
