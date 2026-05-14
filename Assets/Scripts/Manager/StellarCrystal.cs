using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Cristal Estelar que aparece nas ruínas após o jogador completar todas
    /// as ondas da noite 2. Ao encostar nele, o jogador desbloqueia o Corte
    /// Estelar (LMB hold) e a Investida (Q), avança pra noite 3 e a cutscene
    /// de revelação é carregada.
    ///
    /// Setup na cena (Gameplay_Externo):
    ///   - GameObject com SpriteRenderer (Cristal_Sprite_X) + Collider2D trigger
    ///   - Este script
    ///   - GameObject inicia DESATIVADO (SetActive false) — só ativa pós-N2
    ///   - Posicionar num ponto natural do mapa que o jogador precise andar até
    ///
    /// Pra polish: o sprite faz fade-in suave + bobbing + pulse de escala
    /// enquanto está ativo, deixando claro que é interativo.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StellarCrystal : MonoBehaviour
    {
        [Header("Aparição")]
        [Tooltip("Noite em que o cristal deve aparecer ao completar todas as waves. Default 2 (entre N2 e N3).")]
        [SerializeField] private int appearAfterNight = 2;
        [Tooltip("Tempo de fade-in (segundos) quando o cristal aparece pela primeira vez.")]
        [SerializeField] private float fadeInDuration = 1.5f;

        [Header("Transição")]
        [Tooltip("Cena da cutscene de revelação. Será carregada quando o player tocar no cristal.")]
        [SerializeField] private string cutsceneScene = "Cutscene_Noite3";
        [Tooltip("Tag do player (quem ativa o cristal ao encostar).")]
        [SerializeField] private string playerTag = "Player";

        [Header("Visual idle (depois de ativar)")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 1.5f;
        [SerializeField] private float pulseAmplitude = 0.08f;
        [SerializeField] private float pulseSpeed = 2.0f;

        [Header("Som")]
        [SerializeField] private AudioClip appearSound;
        [SerializeField] private AudioClip touchSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        [Header("Light2D (filho do cristal — opcional)")]
        [Tooltip("Pulse de intensidade da luz (amplitude). 0 = luz constante.")]
        [SerializeField] private float lightPulseAmplitude = 0.4f;
        [SerializeField] private float lightPulseSpeed = 2f;

        [Header("Indicador (seta apontando pro cristal — opcional)")]
        [Tooltip("GameObject (geralmente uma seta flutuante) que aparece quando o cristal é revelado, e some quando o jogador toca nele. Pode ser filho do cristal ou outro.")]
        [SerializeField] private GameObject arrowIndicator;

        private Collider2D col;
        private SpriteRenderer[] sprites;
        private float[] spriteBaseAlphas;
        private ParticleSystem[] particles;
        private Light2D crystalLight;
        private float lightBaseIntensity;
        private bool isRevealed;
        private bool consumed;
        private Vector3 baseLocalPos;
        private Vector3 baseLocalScale;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            baseLocalPos = transform.localPosition;
            baseLocalScale = transform.localScale;

            // Pega TODOS os SpriteRenderers (cristal + halo) e ParticleSystems (estrelas)
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            spriteBaseAlphas = new float[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteBaseAlphas[i] = sprites[i].color.a;
                var c = sprites[i].color; c.a = 0f; sprites[i].color = c;
            }

            particles = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                var emission = ps.emission;
                emission.enabled = false;
            }

            // Light2D (se existir como filho)
            crystalLight = GetComponentInChildren<Light2D>(true);
            if (crystalLight != null)
            {
                lightBaseIntensity = crystalLight.intensity;
                crystalLight.intensity = 0f; // começa apagado
            }

            // Indicador começa oculto — só aparece junto do reveal
            if (arrowIndicator != null) arrowIndicator.SetActive(false);

            col.enabled = false;
        }

        private void OnEnable()
        {
            if (WaveManager.Instance != null) Subscribe();
            else Invoke(nameof(Subscribe), 0.05f);
        }

        private void Subscribe()
        {
            if (WaveManager.Instance == null) return;
            WaveManager.Instance.OnAllWavesCompleted += HandleAllWavesCompleted;
        }

        private void OnDisable()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted -= HandleAllWavesCompleted;
        }

        private void HandleAllWavesCompleted()
        {
            if (isRevealed) return;
            int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1;
            if (night != appearAfterNight) return;
            // Pequeno delay pra dar respiro ao "VÁ ATÉ O CRISTAL" / fim da última onda
            StartCoroutine(RevealRoutine());
        }

        private IEnumerator RevealRoutine()
        {
            isRevealed = true;
            yield return new WaitForSeconds(0.8f);

            if (appearSound != null)
                AudioSource.PlayClipAtPoint(appearSound, transform.position, soundVolume);

            // Liga emissão de partículas no início do fade pra elas crescerem junto
            foreach (var ps in particles)
            {
                var emission = ps.emission;
                emission.enabled = true;
                ps.Play();
            }

            // Fade-in de TODOS os sprites (cristal + halo) + Light2D, proporcional ao base de cada
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeInDuration);
                for (int i = 0; i < sprites.Length; i++)
                {
                    var c = sprites[i].color;
                    c.a = k * spriteBaseAlphas[i];
                    sprites[i].color = c;
                }
                if (crystalLight != null)
                    crystalLight.intensity = k * lightBaseIntensity;
                yield return null;
            }
            for (int i = 0; i < sprites.Length; i++)
            {
                var c = sprites[i].color;
                c.a = spriteBaseAlphas[i];
                sprites[i].color = c;
            }
            if (crystalLight != null) crystalLight.intensity = lightBaseIntensity;
            col.enabled = true;

            // Ativa o indicador (seta flutuante) só DEPOIS que o cristal apareceu
            if (arrowIndicator != null) arrowIndicator.SetActive(true);
        }

        private void Update()
        {
            if (!isRevealed || consumed) return;
            // Bobbing + pulse pra deixar o cristal "vivo" enquanto espera o player
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            transform.localPosition = baseLocalPos + new Vector3(0f, bob, 0f);
            transform.localScale = baseLocalScale * pulse;
            // Pulse da luz (respiração)
            if (crystalLight != null && lightPulseAmplitude > 0f)
            {
                float lightPulse = Mathf.Sin(Time.time * lightPulseSpeed) * lightPulseAmplitude;
                crystalLight.intensity = Mathf.Max(0f, lightBaseIntensity + lightPulse);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed) return;
            if (!other.CompareTag(playerTag)) return;
            consumed = true;
            col.enabled = false;
            if (arrowIndicator != null) arrowIndicator.SetActive(false);

            if (touchSound != null)
                AudioSource.PlayClipAtPoint(touchSound, transform.position, soundVolume);

            // Desbloqueia poderes + avança noite + carrega cutscene
            GameManager.Instance?.UnlockStellarPowers();
            GameManager.Instance?.AdvanceNight();
            SceneFader.GetOrCreate().FadeOutAndLoad(cutsceneScene);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, 0.6f);
        }
    }
}
