using UnityEngine;
using TowerDefense.Common;
using TowerDefense.Manager;

namespace TowerDefense.Enemies
{
    /// <summary>
    /// Goblin do Sprint 2 com FSM simples baseada em distância:
    /// - IDLE   : player além de detectionRange → fica parado, anima Idle
    /// - WALK   : player dentro de detectionRange → persegue no eixo X
    /// - ATTACK : player dentro de attackRange → para e ataca por cooldown
    /// - DEAD   : zera HP → para tudo, anima Death; Health destrói o GameObject
    ///
    /// O sprite faz flip pra olhar pro player.
    ///
    /// Parâmetros do Animator (opcionais — só seta se existirem):
    ///   - Speed  (float)   — magnitude da velocidade
    ///   - Attack (trigger) — disparado a cada golpe
    ///   - Death  (trigger) — disparado ao morrer
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class Goblin : MonoBehaviour, IFortressDamager
    {
        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 3f;
        [Tooltip("Posição X muito à esquerda — fallback caso o goblin passe pela fortaleza. Normal: fortaleza captura via trigger antes disso.")]
        [SerializeField] private float despawnX = -25f;

        [Header("Detecção e combate")]
        [Tooltip("Distância no eixo X em que o goblin 'acorda' e começa a perseguir.")]
        [SerializeField] private float detectionRange = 5f;
        [Tooltip("Distância no eixo X em que o goblin para e ataca.")]
        [SerializeField] private float attackRange = 1.1f;
        [Tooltip("Diferença máxima de altura (Y) entre goblin e player pra que o ataque acerte. Player em plataforma acima escapa.")]
        [SerializeField] private float attackHeight = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private int damage = 1;
        [SerializeField] private string playerTag = "Player";

        [Header("Visual")]
        [Tooltip("O sprite olha pra direita por padrão? Se sim, o flip espelha quando o player está à esquerda.")]
        [SerializeField] private bool spriteFacesRight = true;

        [Header("Animator")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string deathTrigger = "Death";

        [Header("Som")]
        [Tooltip("Som tocado quando o goblin morre.")]
        [SerializeField] private AudioClip deathSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        [Header("Recompensa")]
        [Tooltip("Ouro dado ao jogador quando o goblin morre.")]
        [SerializeField] private int goldReward = 5;

        [Header("Dano na fortaleza")]
        [Tooltip("Quanto dano esse inimigo causa ao HP da fortaleza ao chegar nela.")]
        [SerializeField] private int fortressDamage = 2;
        public int FortressDamage => fortressDamage;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Animator animator;
        private AnimatorParamCache animParams;
        private Health health;
        private Knockback knockback;
        private Transform player;
        private float lastAttackTime = -999f;
        // Quando true, o goblin foca o player (perseguir/atacar) em vez de ir pro castelo.
        // Vira true ao tomar dano do player; sai automaticamente se o player se afasta
        // pra fora de detectionRange.
        private bool aggroedOnPlayer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            animParams = new AnimatorParamCache(animator);
            health = GetComponent<Health>();
            knockback = GetComponent<Knockback>();

            if (health != null)
            {
                health.Died += OnDeath;
                // Tomou dano? Vai atrás do player.
                health.Damaged += OnTookDamage;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= OnDeath;
                health.Damaged -= OnTookDamage;
            }
        }

        private void OnTookDamage(int current, int max)
        {
            // Qualquer hit faz o goblin agroar no player
            aggroedOnPlayer = true;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
            {
                player = p.transform;
                // Ignora colisão física entre goblin e player.
                // Sem isso, os colisores se empurram quando se sobrepõem,
                // o que causa o "empurrar eternamente" depois que o player morre.
                var myCol = GetComponent<Collider2D>();
                var playerCol = p.GetComponent<Collider2D>();
                if (myCol != null && playerCol != null)
                {
                    Physics2D.IgnoreCollision(myCol, playerCol, true);
                }
            }
        }

        private void FixedUpdate()
        {
            // Morto: para tudo
            if (health != null && health.IsDead)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                animParams.SetFloat(speedParam, 0f);
                return;
            }

            // Em knockback: não sobrescreve velocidade — deixa o impulso agir
            if (knockback != null && knockback.IsStunned)
            {
                animParams.SetFloat(speedParam, 0f);
                return;
            }

            // Por padrão: o objetivo do goblin é a fortaleza (esquerda do mapa).
            float vx = -moveSpeed;
            float animSpeedValue = moveSpeed;
            bool focusingPlayer = false;

            if (player != null)
            {
                var playerDmg = player.GetComponent<IDamageable>();
                bool playerAlive = playerDmg != null && !playerDmg.IsDead;

                if (playerAlive)
                {
                    float dx = player.position.x - transform.position.x;
                    float distX = Mathf.Abs(dx);
                    float distY = Mathf.Abs(player.position.y - transform.position.y);

                    // Player saiu de alcance? Perde o aggro e volta pro castelo.
                    if (aggroedOnPlayer && distX > detectionRange)
                    {
                        aggroedOnPlayer = false;
                    }

                    bool inAttackRange = distX <= attackRange && distY <= attackHeight;

                    if (inAttackRange)
                    {
                        // Player no alcance de ataque: para e bate, independente de aggro
                        focusingPlayer = true;
                        vx = 0f;
                        animSpeedValue = 0f;
                        UpdateFacing(dx);

                        if (Time.time - lastAttackTime >= attackCooldown)
                        {
                            lastAttackTime = Time.time;
                            animParams.SetTrigger(attackTrigger);
                            playerDmg.TakeDamage(damage);
                        }
                    }
                    else if (aggroedOnPlayer && distX <= detectionRange)
                    {
                        // Aggrod e player dentro do alcance de detecção: persegue.
                        focusingPlayer = true;
                        float dirX = Mathf.Sign(dx);
                        if (dirX == 0f) dirX = -1f;
                        vx = dirX * moveSpeed;
                        animSpeedValue = moveSpeed;
                        UpdateFacing(dx);
                    }
                }
            }

            // Quando NÃO está focando o player, vira pra esquerda (rumo à fortaleza)
            if (!focusingPlayer && sr != null)
            {
                sr.flipX = spriteFacesRight;
            }

            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            animParams.SetFloat(speedParam, animSpeedValue);

            if (transform.position.x <= despawnX)
            {
                // TODO Sprint 3: dano à Fortaleza antes de destruir
                Destroy(gameObject);
            }
        }

        private void UpdateFacing(float dx)
        {
            if (sr == null) return;
            if (Mathf.Abs(dx) < 0.01f) return; // mantém o flip atual se em cima do player
            // Sprite faces right por padrão → flipX=true vira pra esquerda.
            bool playerOnRight = dx > 0f;
            sr.flipX = spriteFacesRight ? !playerOnRight : playerOnRight;
        }

        private void OnDeath()
        {
            animParams.SetTrigger(deathTrigger);
            // Toca o som via PlayClipAtPoint pra continuar mesmo depois que o goblin for destruído
            if (deathSound != null)
                AudioSource.PlayClipAtPoint(deathSound, transform.position, soundVolume);

            // Recompensa de ouro pelo kill
            GameManager.Instance?.AddGold(goldReward);
            // Notifica o WaveManager pra contar a baixa
            WaveManager.Instance?.RegisterEnemyDefeated();
            // Health.cs destrói o GameObject após o destroyDelay
        }

        private void OnDrawGizmosSelected()
        {
            // Amarelo = detecção, vermelho = ataque
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
