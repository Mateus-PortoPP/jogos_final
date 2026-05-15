using UnityEngine;
using TowerDefense.Common;
using TowerDefense.Manager;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// Variante "tank" do goblin — aparece a partir da noite 3.
    /// Diferenças do Goblin comum:
    ///   - Mais lento, mais HP, mais dano
    ///   - Quando avista o jogador dentro do detectionRange, PARA e toca a
    ///     animação "Goblin_Arm_Agressivo" por aggressiveRoarDuration, e SÓ
    ///     DEPOIS carrega em direção ao player (com velocidade aumentada).
    ///   - Ao atacar, empurra o jogador (Knockback) alguns blocos.
    ///
    /// FSM:
    ///   Approaching → caminhando rumo ao castelo (esquerda)
    ///   Spotted     → parado, tocando animação Agressivo (rugido)
    ///   Charging    → corre atrás do player; entra em Attack quando perto
    ///   Dead        → para tudo, Health.cs destroi o GameObject
    ///
    /// O AnimatorController do Goblin_Armadura usa Animator.Play(stateName) por
    /// nome — não depende de parâmetros configurados no controller.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class GoblinArmadura : MonoBehaviour, IFortressDamager
    {
        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 1.8f;
        [Tooltip("Multiplicador aplicado ao moveSpeed quando o monstro está em modo Charging (atrás do player).")]
        [SerializeField] private float chargeSpeedMultiplier = 1.4f;
        [Tooltip("Posição X muito à esquerda — fallback caso passe pela fortaleza. Fortress.trigger pega ele antes disso.")]
        [SerializeField] private float despawnX = -25f;

        [Header("Detecção e combate")]
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float attackHeight = 1.5f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int damage = 3;
        [SerializeField] private string playerTag = "Player";

        [Header("Comportamento agressivo")]
        [Tooltip("Duração da animação de rugido antes do monstro começar a correr pro player.")]
        [SerializeField] private float aggressiveRoarDuration = 1f;

        [Header("Knockback no jogador")]
        [SerializeField] private float knockbackForce = 10f;
        [Tooltip("Componente vertical do empurrão. >0 joga o player um pouco pra cima.")]
        [SerializeField] private float knockbackUp = 0.5f;
        [SerializeField] private float knockbackStun = 0.35f;

        [Header("Visual")]
        [SerializeField] private bool spriteFacesRight = true;

        [Header("Animator (state names — Animator.Play por nome)")]
        [SerializeField] private string idleState = "Goblin_Arm_Idle";
        [SerializeField] private string walkState = "Goblin_Arm_Walk";
        [SerializeField] private string attackState = "Goblin_Arm_Attack";
        [SerializeField] private string aggressiveState = "Goblin_Arm_Agressivo";
        [SerializeField] private string deathState = "Goblin_Arm_Death";

        [Header("Som")]
        [SerializeField] private AudioClip aggressiveSound;
        [SerializeField] private AudioClip deathSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        [Header("Recompensa")]
        [SerializeField] private int goldReward = 15;

        [Header("Dano na fortaleza")]
        [Tooltip("Quanto dano esse inimigo causa ao HP da fortaleza ao chegar nela. Armadura dá mais que goblin comum.")]
        [SerializeField] private int fortressDamage = 6;
        public int FortressDamage => fortressDamage;

        private enum BehaviorState { Approaching, Roaring, Attacking, Dead }
        private BehaviorState behavior = BehaviorState.Approaching;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Animator animator;
        private Health health;
        private Knockback knockback;
        private Transform player;
        private float roarEndTime;
        private float lastAttackTime = -999f;
        private string currentStateName;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            health = GetComponent<Health>();
            knockback = GetComponent<Knockback>();

            if (health != null) health.Died += OnDeath;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDeath;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
            {
                player = p.transform;
                // Ignora colisão física com o player (evita o "empurrar eterno")
                var myCol = GetComponent<Collider2D>();
                var playerCol = p.GetComponent<Collider2D>();
                if (myCol != null && playerCol != null)
                    Physics2D.IgnoreCollision(myCol, playerCol, true);
            }
            PlayState(walkState);
        }

        private void FixedUpdate()
        {
            if (behavior == BehaviorState.Dead) return;
            if (health != null && health.IsDead) { behavior = BehaviorState.Dead; return; }

            // Em knockback (player bateu): não sobrescreve a velocidade do empurrão
            if (knockback != null && knockback.IsStunned) return;

            float dx = 0f, distX = float.PositiveInfinity, distY = float.PositiveInfinity;
            bool playerAlive = false;
            if (player != null)
            {
                var pdmg = player.GetComponent<IDamageable>();
                playerAlive = pdmg != null && !pdmg.IsDead;
                dx = player.position.x - transform.position.x;
                distX = Mathf.Abs(dx);
                distY = Mathf.Abs(player.position.y - transform.position.y);
            }
            bool inAttackRange = playerAlive && distX <= attackRange && distY <= attackHeight;

            switch (behavior)
            {
                case BehaviorState.Approaching:
                    MoveToCastle();
                    // Só reage se o player estiver BLOQUEANDO o caminho (colado).
                    // Não corre atrás do player pelo mapa — objetivo é o castelo.
                    if (inAttackRange) EnterRoar(dx);
                    break;

                case BehaviorState.Roaring:
                    // Parado rugindo, encarando o player. Depois parte pro ataque.
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    UpdateFacing(dx);
                    if (Time.time >= roarEndTime)
                        behavior = BehaviorState.Attacking;
                    break;

                case BehaviorState.Attacking:
                    if (inAttackRange)
                    {
                        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                        UpdateFacing(dx);
                        TryAttack(dx);
                    }
                    else
                    {
                        // Player saiu da frente: volta a focar o castelo.
                        behavior = BehaviorState.Approaching;
                    }
                    break;
            }

            // Despawn se passou da fortaleza — conta como "chegou no castelo"
            // (dano + baixa) mesmo se o trigger da Fortress falhar.
            if (transform.position.x <= despawnX)
                ReachCastleAndDespawn();
        }

        private bool castleReached;
        private void ReachCastleAndDespawn()
        {
            if (castleReached) return;
            castleReached = true;
            GameManager.Instance?.TakeFortressDamage(fortressDamage);
            WaveManager.Instance?.RegisterEnemyReachedCastle();
            Destroy(gameObject);
        }

        private void MoveToCastle()
        {
            rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
            if (sr != null) sr.flipX = spriteFacesRight;
            PlayState(walkState);
        }

        private void EnterRoar(float dx)
        {
            behavior = BehaviorState.Roaring;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            roarEndTime = Time.time + aggressiveRoarDuration;
            UpdateFacing(dx);
            PlayState(aggressiveState);
            if (aggressiveSound != null)
                AudioSource.PlayClipAtPoint(aggressiveSound, transform.position, soundVolume);
        }

        private void TryAttack(float dx)
        {
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;
            PlayState(attackState);

            if (player == null) return;
            var pdmg = player.GetComponent<IDamageable>();
            if (pdmg != null && !pdmg.IsDead)
            {
                pdmg.TakeDamage(damage);
                ApplyKnockbackToPlayer(dx);
            }
        }

        private void ApplyKnockbackToPlayer(float dx)
        {
            if (player == null) return;
            // dx é a posição do player relativa ao monstro. Empurra o player NA
            // DIREÇÃO oposta ao monstro (se player está à direita, empurra pra direita).
            float dir = dx >= 0f ? 1f : -1f;
            Vector2 force = new Vector2(dir, knockbackUp);

            var pkb = player.GetComponent<Knockback>();
            if (pkb != null)
            {
                pkb.Apply(force, knockbackForce, knockbackStun);
                return;
            }

            // Fallback: empurra direto no Rigidbody se o player não tem Knockback.
            var prb = player.GetComponent<Rigidbody2D>();
            if (prb != null)
                prb.linearVelocity = force.normalized * knockbackForce;
        }

        private void UpdateFacing(float dx)
        {
            if (sr == null) return;
            if (Mathf.Abs(dx) < 0.01f) return;
            bool playerOnRight = dx > 0f;
            sr.flipX = spriteFacesRight ? !playerOnRight : playerOnRight;
        }

        private void PlayState(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            if (currentStateName == stateName) return;
            currentStateName = stateName;
            animator.Play(stateName);
        }

        private void OnDeath()
        {
            behavior = BehaviorState.Dead;
            // Força a animação de morte (não usa currentStateName guard pra
            // garantir que toque mesmo se já estiver no estado).
            if (animator != null && !string.IsNullOrEmpty(deathState))
                animator.Play(deathState);
            if (deathSound != null)
                AudioSource.PlayClipAtPoint(deathSound, transform.position, soundVolume);

            GameManager.Instance?.AddGold(goldReward);
            WaveManager.Instance?.RegisterEnemyDefeated();
            // Health.cs destrói o GameObject após destroyDelay.
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
