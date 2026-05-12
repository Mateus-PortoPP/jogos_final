using System.Collections;
using TMPro;
using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Banner central animado que anuncia eventos de wave/noite.
    ///
    /// Coloque num GameObject filho do HUD Canvas com:
    ///   - TextMeshProUGUI (referenciado em 'text')
    ///   - CanvasGroup (referenciado em 'group')
    /// Posicione no centro da tela, font size grande (~80), com outline preto.
    ///
    /// Escuta automaticamente o WaveManager.Instance se ele existir na cena.
    /// </summary>
    public class WaveBanner : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private CanvasGroup group;

        [Header("Animação")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float holdDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float startScale = 0.7f;
        [SerializeField] private float endScale = 1.0f;

        [Header("Mensagens")]
        [SerializeField] private string waveStartFormat = "ONDA {0}";
        [SerializeField] private string waveCompleteFormat = "ONDA {0} COMPLETA";
        [SerializeField] private string nightCompleteMessage = "NOITE COMPLETA";
        [SerializeField] private string goToDoorMessage = "VÁ ATÉ A PORTA";

        private Coroutine current;
        private RectTransform rt;

        private void Awake()
        {
            rt = transform as RectTransform;
            if (group != null) group.alpha = 0f;
        }

        private void OnEnable()
        {
            if (WaveManager.Instance != null) Subscribe(WaveManager.Instance);
            else StartCoroutine(WaitAndSubscribe());
        }

        private IEnumerator WaitAndSubscribe()
        {
            // WaveManager pode acordar 1 frame depois — espera ele aparecer.
            while (WaveManager.Instance == null) yield return null;
            Subscribe(WaveManager.Instance);
        }

        private void Subscribe(WaveManager wm)
        {
            wm.OnWaveStarted += HandleWaveStarted;
            wm.OnWaveCompleted += HandleWaveCompleted;
            wm.OnAllWavesCompleted += HandleAllWavesCompleted;
        }

        private void OnDisable()
        {
            if (WaveManager.Instance == null) return;
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
            WaveManager.Instance.OnWaveCompleted -= HandleWaveCompleted;
            WaveManager.Instance.OnAllWavesCompleted -= HandleAllWavesCompleted;
        }

        private void HandleWaveStarted(int idx)  => Show(string.Format(waveStartFormat, idx + 1));
        private void HandleWaveCompleted(int idx) => Show(string.Format(waveCompleteFormat, idx + 1));

        private void HandleAllWavesCompleted()
        {
            // Se for o fim da N2 (interno), instrui o jogador a ir até a porta.
            int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 0;
            Show(night == 2 ? goToDoorMessage : nightCompleteMessage);
        }

        /// <summary>Exibe uma mensagem custom — pode ser chamado de fora também.</summary>
        public void Show(string message)
        {
            if (text == null || group == null) return;
            text.text = message;
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            // Fade in + scale
            float t = 0f;
            group.alpha = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeInDuration);
                group.alpha = k;
                if (rt != null) rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);
                yield return null;
            }
            group.alpha = 1f;
            if (rt != null) rt.localScale = Vector3.one * endScale;

            // Hold
            yield return new WaitForSeconds(holdDuration);

            // Fade out
            t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration);
                yield return null;
            }
            group.alpha = 0f;
            current = null;
        }
    }
}
