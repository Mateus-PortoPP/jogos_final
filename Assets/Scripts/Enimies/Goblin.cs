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
        [Tooltip("Acréscimo de velocidade por onda dentro da noite (rampa interna).")]
        [SerializeField] private float speedPerWave = 0f;
        [Tooltip("Acréscimo de velocidade por noite. Como a noite só sobe, a velocidade NÃO volta ao normal quando a noite vira — ela acumula até o teto e se mantém.")]
        [SerializeField] private float speedPerNight = 0f;
        [Tooltip("Velocidade máxima (teto). Depois de atingir, mantém.")]
        [SerializeField] private float maxMoveSpeed = 5f;
        [Tooltip("Posição X muito à esquerda — fallback caso o goblin passe pela fortaleza. Normal: fortaleza captura via trigger antes disso.")]
        [SerializeField] private float despawnX = -25f;

        [Header("Comportamento")]
        [Tooltip("Modo minion de Tower Defense: ignora 100% o player, nunca para pra atacar — só corre pra fortaleza. O player/canhões precisam matá-lo. Ligar só no goblin pequeno.")]
        [SerializeField] private bool pureRusher = false;

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
        [Tooltip("Força de empurrão aplicada ao player no ataque (0 = nenhum). Boss usa pra arremessar o jogador.")]
        [SerializeField] private float attackKnockbackForce = 0f;
        [SerializeField] private float attackKnockbackUp = 0.4f;
        [SerializeField] private float attackKnockbackStun = 0.25f;

        [Header("Ataque à distância (opcional)")]
        [Tooltip("Se setado, o inimigo para a essa distância e arremessa esse projétil no player em vez de ir pro corpo a corpo.")]
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private float rangedRange = 7f;
        [SerializeField] private float rangedHeight = 3f;
        [SerializeField] private float rangedCooldown = 2.2f;
        [SerializeField] private float fireballSpeed = 9f;
        [SerializeField] private int fireballDamage = 3;
        [SerializeField] private Vector2 fireballSpawnOffset = new Vector2(0.6f, 0.3f);

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

        [Header("Foco no castelo")]
        [Tooltip("Tempo (s) que o goblin persegue o player após apanhar. Depois disso volta a marchar pro castelo. Menor = mais focado no castelo.")]
        [SerializeField] private float aggroDuration = 1.5f;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Animator animator;
        private AnimatorParamCache animParams;
        private Health health;
        private Knockback knockback;
        private Transform player;
        private float lastAttackTime = -999f;
        private float lastRangedTime = -999f;
        private float aggroEndTime = -999f;
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
            // Apanhou: reage e persegue o player por um tempo CURTO, depois
            // volta a focar o castelo (objetivo principal).
            aggroedOnPlayer = true;
            aggroEndTime = Time.time + aggroDuration;
        }

        private void Start()
        {
            // Velocidade escala com a NOITE (monotônico — nunca reseta quando a
            // noite vira) + uma rampa interna por onda. Tudo limitado por maxMoveSpeed.
            if (speedPerWave > 0f || speedPerNight > 0f)
            {
                int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1;
                int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveNumber : 1;
                float bonus = (night - 1) * speedPerNight + (wave - 1) * speedPerWave;
                moveSpeed = Mathf.Min(maxMoveSpeed, moveSpeed + bonus);
            }

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
            // Blindagem contra hot-reload (animParams null se Awake não rodou nessa instância)
            if (animParams == null) animParams = new AnimatorParamCache(GetComponent<Animator>());
            if (rb == null) rb = GetComponent<Rigidbody2D>();

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

            // Modo rusher (minion TD): NÃO para nem persegue, mas se o player
            // ficar no caminho ele dá um hit "de passagem" (continua marchando).
            if (pureRusher && player != null)
            {
                var pd = player.GetComponent<IDamageable>();
                if (pd != null && !pd.IsDead)
                {
                    float ddx = Mathf.Abs(player.position.x - transform.position.x);
                    float ddy = Mathf.Abs(player.position.y - transform.position.y);
                    if (ddx <= attackRange && ddy <= attackHeight
                        && Time.time - lastAttackTime >= attackCooldown)
                    {
                        lastAttackTime = Time.time;
                        animParams.SetTrigger(attackTrigger);
                        pd.TakeDamage(damage); // dano de esbarrão, sem parar a marcha
                    }
                }
            }

            // Sem rusher: comportamento normal (para/ataca quem bloqueia).
            if (!pureRusher && player != null)
            {
                var playerDmg = player.GetComponent<IDamageable>();
                bool playerAlive = playerDmg != null && !playerDmg.IsDead;

                if (playerAlive)
                {
                    float dx = player.position.x - transform.position.x;
                    float distX = Mathf.Abs(dx);
                    float distY = Mathf.Abs(player.position.y - transform.position.y);

                    // Perde o aggro (e volta a marchar pro castelo) se o player
                    // saiu de alcance OU se o tempo de aggro acabou. O ataque
                    // corpo a corpo (inAttackRange) continua valendo independente disso.
                    if (aggroedOnPlayer && (distX > detectionRange || Time.time > aggroEndTime))
                    {
                        aggroedOnPlayer = false;
                    }

                    // Caster: continua marchando pro castelo, mas arremessa projétil
                    // no player enquanto avança (não para, não persegue).
                    bool didRanged = false;
                    if (fireballPrefab != null && distX <= rangedRange && distY <= rangedHeight)
                    {
                        didRanged = true;
                        focusingPlayer = true; // vira pro player só pra mirar; vx continua rumo ao castelo
                        UpdateFacing(dx);

                        if (Time.time - lastRangedTime >= rangedCooldown)
                        {
                            lastRangedTime = Time.time;
                            animParams.SetTrigger(attackTrigger);
                            ThrowFireball(player);
                        }
                    }

                    bool inAttackRange = !didRanged && distX <= attackRange && distY <= attackHeight;

                    if (inAttackRange)
                    {
                        // Player bloqueando o caminho: para e bate. NÃO persegue —
                        // se o player sair do attackRange, volta a marchar pro castelo.
                        focusingPlayer = true;
                        vx = 0f;
                        animSpeedValue = 0f;
                        UpdateFacing(dx);

                        if (Time.time - lastAttackTime >= attackCooldown)
                        {
                            lastAttackTime = Time.time;
                            animParams.SetTrigger(attackTrigger);
                            playerDmg.TakeDamage(damage);

                            if (attackKnockbackForce > 0f)
                            {
                                var playerKb = player.GetComponent<Knockback>();
                                if (playerKb != null)
                                {
                                    float dirX = transform.position.x < player.position.x ? 1f : -1f;
                                    playerKb.Apply(new Vector2(dirX, attackKnockbackUp), attackKnockbackForce, attackKnockbackStun);
                                }
                            }
                        }
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
                ReachCastleAndDespawn();
            }
        }

        // Fallback: se o inimigo passou batido pelo trigger da Fortress e chegou
        // no despawnX, ele AINDA conta como "chegou no castelo" (dano + baixa no
        // WaveManager). Sem isso a onda nunca termina se o trigger falhar.
        private bool castleReached;
        private void ReachCastleAndDespawn()
        {
            if (castleReached) return;
            castleReached = true;
            GameManager.Instance?.TakeFortressDamage(fortressDamage);
            WaveManager.Instance?.RegisterEnemyReachedCastle();
            Destroy(gameObject);
        }

        private void ThrowFireball(Transform target)
        {
            if (fireballPrefab == null || target == null) return;
            float facing = target.position.x >= transform.position.x ? 1f : -1f;
            Vector3 spawn = transform.position + new Vector3(fireballSpawnOffset.x * facing, fireballSpawnOffset.y, 0f);
            var fb = Instantiate(fireballPrefab, spawn, Quaternion.identity);
            var proj = fb.GetComponent<CannonProjectile>();
            if (proj != null)
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)spawn).normalized;
                proj.Launch(dir, fireballSpeed, fireballDamage, playerTag);
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
