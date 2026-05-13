using UnityEngine;
using UnityEngine.InputSystem;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// Ataque corpo a corpo do Guardião com duas variantes no mesmo botão:
    ///   - Clique rápido (segurar < stellarMinChargeTime): ataque normal (hitbox curta).
    ///   - Segurar e soltar (>= stellarMinChargeTime): CORTE ESTELAR — rajada de
    ///     energia com hitbox maior, mais dano (escala com tempo de carga) e
    ///     knockback mais forte.
    ///
    /// Setup:
    ///   - Esse script no Player, junto de PlayerController + Animator + SpriteRenderer.
    ///   - Adicionar parâmetros opcionais no Animator: "Attack" (trigger), "StellarCut"
    ///     (trigger), "IsChargingCut" (bool). Se não existirem, são ignorados.
    ///   - Arrastar VFX_CorteEstelar.prefab pro slot stellarVfxPrefab.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Ataque normal")]
        [SerializeField] private int damage = 25;
        [SerializeField] private float attackCooldown = 0.6f;
        [SerializeField] private string targetTag = "Enemy";

        [Header("Hitbox (ataque normal)")]
        [Tooltip("Offset da hitbox em relação ao centro do player (X positivo = à frente).")]
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.7f, 0.4f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.0f);

        [Header("Knockback (ataque normal)")]
        [SerializeField] private float knockbackForce = 6f;
        [SerializeField] private float knockbackUp = 0.3f;

        [Header("Corte Estelar — Charge")]
        [Tooltip("Tempo mínimo de hold (segundos) pra disparar o Corte Estelar em vez do ataque normal.")]
        [SerializeField] private float stellarMinChargeTime = 3f;
        [Tooltip("Tempo de hold pra atingir dano máximo. Cargas maiores que isso não escalam mais.")]
        [SerializeField] private float stellarMaxChargeTime = 5f;

        [Header("Corte Estelar — Dano")]
        [SerializeField] private int stellarMinDamage = 50;
        [SerializeField] private int stellarMaxDamage = 150;
        [Tooltip("Hitbox AoE do corte (área de efeito da rajada).")]
        [SerializeField] private Vector2 stellarHitboxSize = new Vector2(4f, 1.5f);
        [SerializeField] private Vector2 stellarHitboxOffset = new Vector2(1.8f, 0.4f);

        [Header("Corte Estelar — Knockback")]
        [SerializeField] private float stellarKnockbackForce = 16f;
        [SerializeField] private float stellarKnockbackUp = 0.4f;
        [SerializeField] private float stellarKnockbackStun = 0.45f;

        [Header("Corte Estelar — VFX & Som")]
        [SerializeField] private GameObject stellarVfxPrefab;
        [Tooltip("Offset local INICIAL do VFX em relação ao player (X flipado pelo facing). O VFX viaja pra frente a partir daqui.")]
        [SerializeField] private Vector3 stellarVfxOffset = new Vector3(0.7f, 0.3f, 0f);
        [Tooltip("Velocidade horizontal (unidades/s) que o VFX viaja pra frente durante seu lifetime. 0 = parado.")]
        [SerializeField] private float stellarVfxSpeed = 4f;
        [Tooltip("Quanto tempo o VFX da rajada vive antes de ser destruído. Deve ser >= duração do clipe da animação.")]
        [SerializeField] private float stellarVfxLifetime = 0.8f;
        [Tooltip("Partículas/aura instanciadas como filho do player ENQUANTO ele segura o botão. Destruídas no release.")]
        [SerializeField] private GameObject chargeParticlesPrefab;
        [SerializeField] private AudioClip stellarChargeClip;
        [SerializeField] private AudioClip stellarReleaseClip;

        [Header("Animator")]
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string stellarCutTrigger = "StellarCut";
        [SerializeField] private string chargingBoolParam = "IsChargingCut";

        [Header("Som — Ataque normal")]
        [SerializeField] private AudioClip swordSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        private SpriteRenderer sr;
        private Animator animator;
        private AnimatorParamCache animParams;
        private PlayerController controller;
        private float lastAttackTime = -999f;
        private float chargeStartTime = -1f;
        private bool isCharging;
        private bool stellarReady; // vira true quando a carga cruza stellarMinChargeTime
        private AudioSource chargeAudioSource;
        private GameObject activeChargeParticles;

        // O PlayerInput está em modo SendMessages, que só forward o callback de
        // 'performed' (press), não o de 'canceled' (release). Por isso polamos a
        // InputAction diretamente em Update — funciona em todas as bindings (mouse,
        // gamepad, touch, etc.) e detecta press E release.
        private UnityEngine.InputSystem.InputAction attackAction;

        public bool IsChargingStellarCut => isCharging;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            animParams = new AnimatorParamCache(animator);
            controller = GetComponent<PlayerController>();

            var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null && pi.actions != null) attackAction = pi.actions.FindAction("Attack");
        }

        private void Update()
        {
            if (attackAction != null)
            {
                if (attackAction.WasPressedThisFrame()) BeginCharge();
                if (attackAction.WasReleasedThisFrame()) ReleaseCharge();
            }
            else
            {
                // Fallback: pulha Mouse direto se PlayerInput não estiver configurado.
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    if (mouse.leftButton.wasPressedThisFrame) BeginCharge();
                    if (mouse.leftButton.wasReleasedThisFrame) ReleaseCharge();
                }
            }

            // Detecta o momento em que a carga cruza o threshold pra ativar o
            // feedback visual (partículas + sprite de carga + audio). Antes disso
            // o player parece normal — só "brilha" quando o Corte Estelar fica disponível.
            if (isCharging && !stellarReady && chargeStartTime >= 0f)
            {
                if (Time.time - chargeStartTime >= stellarMinChargeTime)
                    EnterStellarReady();
            }
        }

        private void EnterStellarReady()
        {
            stellarReady = true;
            animParams.SetBool(chargingBoolParam, true);

            if (chargeParticlesPrefab != null && activeChargeParticles == null)
            {
                activeChargeParticles = Instantiate(chargeParticlesPrefab, transform.position, Quaternion.identity, transform);
                activeChargeParticles.transform.localPosition = Vector3.zero;
            }

            if (stellarChargeClip != null)
            {
                if (chargeAudioSource == null) chargeAudioSource = gameObject.AddComponent<AudioSource>();
                chargeAudioSource.clip = stellarChargeClip;
                chargeAudioSource.loop = true;
                chargeAudioSource.volume = soundVolume;
                chargeAudioSource.Play();
            }
        }

        /// <summary>Aumenta o dano por golpe — usado pelos upgrades do cavaleiro.</summary>
        public void AddDamage(int delta)
        {
            if (delta <= 0) return;
            damage += delta;
            stellarMinDamage += delta;
            stellarMaxDamage += delta;
        }

        private void BeginCharge()
        {
            if (isCharging) return;
            if (Time.time - lastAttackTime < attackCooldown) return;

            isCharging = true;
            stellarReady = false;
            chargeStartTime = Time.time;
            // Sem feedback visual ainda — o "brilho" só liga quando a carga
            // cruzar stellarMinChargeTime (ver EnterStellarReady).
        }

        private void ReleaseCharge()
        {
            if (!isCharging) return;
            isCharging = false;
            bool wasReady = stellarReady;
            stellarReady = false;
            if (wasReady) animParams.SetBool(chargingBoolParam, false);
            if (chargeAudioSource != null && chargeAudioSource.isPlaying) chargeAudioSource.Stop();

            // Mata as partículas de carga (com pequeno delay pra terminarem suavemente)
            if (activeChargeParticles != null)
            {
                var ps = activeChargeParticles.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(activeChargeParticles, 0.4f);
                activeChargeParticles = null;
            }

            float chargeTime = Time.time - chargeStartTime;
            chargeStartTime = -1f;
            lastAttackTime = Time.time;

            if (chargeTime >= stellarMinChargeTime)
            {
                PerformStellarCut(chargeTime);
            }
            else
            {
                PerformNormalAttack();
            }
        }

        private void PerformNormalAttack()
        {
            animParams.SetTrigger(attackTrigger);

            if (swordSound != null)
                AudioSource.PlayClipAtPoint(swordSound, transform.position, soundVolume);

            float facing = GetFacingSign();
            Vector2 origin = (Vector2)transform.position + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);
            var hits = Physics2D.OverlapBoxAll(origin, hitboxSize, 0f);

            int hitCount = ApplyDamageAndKnockback(hits, damage, facing, knockbackForce, knockbackUp, -1f);

            if (hitCount > 0 && hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
        }

        private void PerformStellarCut(float chargeTime)
        {
            animParams.SetTrigger(stellarCutTrigger);

            if (stellarReleaseClip != null)
                AudioSource.PlayClipAtPoint(stellarReleaseClip, transform.position, soundVolume);

            float facing = GetFacingSign();

            // Damage escala com a carga (0 = min, 1 = max).
            float chargeSpan = Mathf.Max(0.0001f, stellarMaxChargeTime - stellarMinChargeTime);
            float t = Mathf.Clamp01((chargeTime - stellarMinChargeTime) / chargeSpan);
            int dmg = Mathf.RoundToInt(Mathf.Lerp(stellarMinDamage, stellarMaxDamage, t));

            // Spawn do VFX no offset (flipado por facing). VFX não é filho do player —
            // a rajada fica no lugar; o player pode se mover.
            if (stellarVfxPrefab != null)
            {
                Vector3 worldOffset = new Vector3(stellarVfxOffset.x * facing, stellarVfxOffset.y, stellarVfxOffset.z);
                var vfx = Instantiate(stellarVfxPrefab, transform.position + worldOffset, Quaternion.identity);
                var s = vfx.transform.localScale;
                s.x = Mathf.Abs(s.x) * facing;
                vfx.transform.localScale = s;
                // VFX viaja pra frente durante seu lifetime pra cobrir a área da hitbox.
                if (stellarVfxSpeed > 0f)
                    StartCoroutine(MoveVfx(vfx.transform, new Vector2(stellarVfxSpeed * facing, 0f), stellarVfxLifetime));
                Destroy(vfx, stellarVfxLifetime);
            }

            // Hitbox AoE em frente ao player.
            Vector2 origin = (Vector2)transform.position + new Vector2(stellarHitboxOffset.x * facing, stellarHitboxOffset.y);
            var hits = Physics2D.OverlapBoxAll(origin, stellarHitboxSize, 0f);
            ApplyDamageAndKnockback(hits, dmg, facing, stellarKnockbackForce, stellarKnockbackUp, stellarKnockbackStun);
        }

        private System.Collections.IEnumerator MoveVfx(Transform vfx, Vector2 velocity, float duration)
        {
            float t = 0f;
            while (t < duration && vfx != null)
            {
                vfx.position += (Vector3)(velocity * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }
        }

        private int ApplyDamageAndKnockback(Collider2D[] hits, int dmg, float facing, float kbForce, float kbUp, float kbStun)
        {
            int hitCount = 0;
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (!hit.CompareTag(targetTag)) continue;
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;

                damageable.TakeDamage(dmg);
                hitCount++;

                var kb = hit.GetComponent<Knockback>();
                if (kb != null)
                {
                    Vector2 dir = new Vector2(facing, kbUp);
                    if (kbStun > 0f) kb.Apply(dir, kbForce, kbStun);
                    else kb.Apply(dir, kbForce);
                }
            }
            return hitCount;
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
            // Hitbox ataque normal (vermelho)
            Vector2 origin = (Vector2)transform.position + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(origin, hitboxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(origin, hitboxSize);
            // Hitbox Corte Estelar (azul)
            Vector2 sOrigin = (Vector2)transform.position + new Vector2(stellarHitboxOffset.x * facing, stellarHitboxOffset.y);
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
            Gizmos.DrawCube(sOrigin, stellarHitboxSize);
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 1f);
            Gizmos.DrawWireCube(sOrigin, stellarHitboxSize);
        }
    }
}
