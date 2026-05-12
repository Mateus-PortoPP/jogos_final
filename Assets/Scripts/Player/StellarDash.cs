using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// Investida Estelar — habilidade de dash horizontal com dano.
    /// - Tecla Q
    /// - Avança rápido na direção do facing
    /// - Aplica dano em todos os inimigos na caixa do dash
    /// - Instancia VFX que segue o player durante o dash
    /// - Cooldown padrão de 8s
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class StellarDash : MonoBehaviour
    {
        [Header("Dash")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.35f;
        [SerializeField] private float cooldown = 8f;

        [Header("Dano")]
        [SerializeField] private int damage = 40;
        [SerializeField] private Vector2 hitboxSize = new Vector2(2.5f, 1.2f);
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Knockback")]
        [Tooltip("Força do empurrão aplicado nos inimigos atingidos.")]
        [SerializeField] private float knockbackForce = 14f;
        [Tooltip("Componente vertical do empurrão (positivo = empurra pra cima).")]
        [SerializeField] private float knockbackUpwards = 0.3f;
        [Tooltip("Tempo que o inimigo fica stunned depois do empurrão.")]
        [SerializeField] private float knockbackStun = 0.4f;

        [Header("VFX")]
        [Tooltip("Prefab da animação do dash. Vai virar filho do player durante o dash.")]
        [SerializeField] private GameObject dashVfxPrefab;
        [Tooltip("Offset local do VFX em relação ao player.")]
        [SerializeField] private Vector3 vfxOffset = Vector3.zero;

        [Header("Input")]
        [SerializeField] private Key activationKey = Key.Q;

        private Rigidbody2D rb;
        private PlayerController controller;
        private float cooldownTimer;

        public bool IsDashing { get; private set; }
        public bool IsOnCooldown => cooldownTimer > 0f;
        public float CooldownRemaining => cooldownTimer;
        public float CooldownMax => cooldown;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[activationKey].wasPressedThisFrame && !IsOnCooldown && !IsDashing)
            {
                StartCoroutine(DashRoutine());
            }
        }

        private IEnumerator DashRoutine()
        {
            IsDashing = true;
            cooldownTimer = cooldown;

            float direction = (controller != null && controller.FacingRight) ? 1f : -1f;

            // Instancia o VFX como filho do player pra acompanhar a movimentação.
            GameObject vfxInstance = null;
            if (dashVfxPrefab != null)
            {
                vfxInstance = Instantiate(dashVfxPrefab, transform.position + vfxOffset, Quaternion.identity, transform);
                // Espelha o VFX se o dash for pra esquerda
                var s = vfxInstance.transform.localScale;
                s.x = Mathf.Abs(s.x) * direction;
                vfxInstance.transform.localScale = s;
            }

            // Dash horizontal puro — anula gravidade temporariamente
            float originalGravity = rb.gravityScale;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashSpeed * direction, 0f);

            // Checagem contínua de dano durante o dash. Sem isso, se o player
            // está parado e o goblin não está exatamente na frente, a checagem
            // inicial não pega ninguém e o dash atravessa sem causar dano.
            var damagedEnemies = new HashSet<GameObject>();
            float timer = 0f;
            while (timer < dashDuration)
            {
                DealDashDamage(direction, damagedEnemies);
                timer += Time.deltaTime;
                yield return null;
            }

            rb.gravityScale = originalGravity;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            IsDashing = false;

            // VFX se autodestroi pelo próprio script (AutoDestroyAfter), mas garante limpeza:
            if (vfxInstance != null) Destroy(vfxInstance, 0.5f);
        }

        private void DealDashDamage(float direction, HashSet<GameObject> damagedEnemies)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(hitboxSize.x * direction * 0.5f, 0f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f);

            Vector2 knockDir = new Vector2(direction, knockbackUpwards).normalized;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (!hit.CompareTag(enemyTag)) continue;
                // Não bate duas vezes no mesmo goblin durante o mesmo dash
                if (damagedEnemies.Contains(hit.gameObject)) continue;

                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(damage);
                    damagedEnemies.Add(hit.gameObject);
                }

                var knock = hit.GetComponent<Knockback>();
                if (knock != null)
                    knock.Apply(knockDir, knockbackForce, knockbackStun);
            }
        }

        private void OnDrawGizmosSelected()
        {
            float dir = 1f;
            if (Application.isPlaying && controller != null)
                dir = controller.FacingRight ? 1f : -1f;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Vector3 center = transform.position + new Vector3(hitboxSize.x * dir * 0.5f, 0f, 0f);
            Gizmos.DrawWireCube(center, hitboxSize);
        }
    }
}
