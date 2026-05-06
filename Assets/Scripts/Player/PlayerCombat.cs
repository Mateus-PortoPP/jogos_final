using UnityEngine;
using UnityEngine.InputSystem;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// Ataque corpo a corpo do Guardião. Quando o jogador clica/aperta o botão de ataque:
    /// - Dispara o trigger "Attack" no Animator (se existir)
    /// - Cria uma hitbox virtual (OverlapBox) à frente do player
    /// - Aplica dano em qualquer IDamageable com a tag-alvo dentro da caixa
    ///
    /// Parâmetros do Animator que este script seta (opcional):
    ///   - Attack (trigger) — disparado a cada golpe válido
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Dano")]
        [SerializeField] private int damage = 25;
        [SerializeField] private float attackCooldown = 0.6f;
        [SerializeField] private string targetTag = "Enemy";

        [Header("Hitbox")]
        [Tooltip("Offset da hitbox em relação ao centro do player (X positivo = à frente).")]
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.7f, 0.4f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.0f);

        [Header("Animator")]
        [SerializeField] private string attackTrigger = "Attack";

        [Header("Som")]
        [Tooltip("Som tocado a cada golpe (mesmo no vazio).")]
        [SerializeField] private AudioClip swordSound;
        [Tooltip("Som tocado quando o golpe acerta pelo menos um inimigo.")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        [Header("Knockback")]
        [Tooltip("Força do empurrão aplicado em inimigos atingidos.")]
        [SerializeField] private float knockbackForce = 6f;
        [Tooltip("Componente vertical do empurrão (>0 joga um pouco pra cima).")]
        [SerializeField] private float knockbackUp = 0.3f;

        private SpriteRenderer sr;
        private Animator animator;
        private AnimatorParamCache animParams;
        private PlayerController controller;
        private float lastAttackTime = -999f;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            animParams = new AnimatorParamCache(animator);
            controller = GetComponent<PlayerController>();
        }

        public void OnAttack(InputValue value)
        {
            Debug.Log($"[PlayerCombat] OnAttack chamado. isPressed={value.isPressed}");
            if (!value.isPressed) return;
            if (Time.time - lastAttackTime < attackCooldown)
            {
                Debug.Log("[PlayerCombat] Em cooldown, ignorado.");
                return;
            }

            lastAttackTime = Time.time;
            Debug.Log("[PlayerCombat] Atacando! Disparando trigger e checando hits.");

            animParams.SetTrigger(attackTrigger);

            if (swordSound != null)
                AudioSource.PlayClipAtPoint(swordSound, transform.position, soundVolume);

            float facing = GetFacingSign();
            Vector2 origin = (Vector2)transform.position + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);

            var hits = Physics2D.OverlapBoxAll(origin, hitboxSize, 0f);
            int hitCount = 0;
            foreach (var hit in hits)
            {
                if (!hit.CompareTag(targetTag)) continue;
                var dmg = hit.GetComponent<IDamageable>();
                if (dmg != null && !dmg.IsDead)
                {
                    dmg.TakeDamage(damage);
                    hitCount++;

                    // Empurra o inimigo na direção em que o player está olhando
                    var kb = hit.GetComponent<Knockback>();
                    if (kb != null)
                    {
                        Vector2 dir = new Vector2(facing, knockbackUp);
                        kb.Apply(dir, knockbackForce);
                    }
                }
            }
            Debug.Log($"[PlayerCombat] {hits.Length} colliders na hitbox, {hitCount} inimigos atingidos.");

            if (hitCount > 0 && hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
        }

        private float GetFacingSign()
        {
            if (controller != null) return controller.FacingRight ? 1f : -1f;
            return (sr != null && sr.flipX) ? -1f : 1f;
        }

        private void OnDrawGizmosSelected()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            float facing = GetFacingSign();
            Vector2 origin = (Vector2)transform.position + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawCube(origin, hitboxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(origin, hitboxSize);
        }
    }
}
