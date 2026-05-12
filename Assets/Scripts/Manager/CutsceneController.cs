using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Controla uma cutscene de imagem estática com legenda revelada por input.
    ///
    /// Fluxo:
    ///   - Cena inicia com a imagem visível, a legenda invisível (alpha 0) e o overlay preto invisível.
    ///   - 1º Enter/Espaço: legenda faz fade-in.
    ///   - 2º Enter/Espaço: overlay preto faz fade-out e carrega a próxima cena.
    ///
    /// Tudo configurável por Inspector para reaproveitar nas demais cutscenes
    /// (Cutscene_Noite3, Cutscene_Noite4, Victory).
    /// </summary>
    public class CutsceneController : MonoBehaviour
    {
        [Header("Legenda")]
        [Tooltip("Texto que aparece quando o jogador aperta Enter pela primeira vez.")]
        [SerializeField, TextArea(3, 10)] private string captionContent =
            "Numa noite de eclipse, os goblins furaram as muralhas do castelo.\n" +
            "O Guardião precisa expulsá-los antes que cheguem ao Rei.\n\n" +
            "<i>Pressione Enter para começar.</i>";

        [SerializeField] private TextMeshProUGUI captionText;
        [SerializeField] private CanvasGroup captionGroup;
        [SerializeField] private float captionFadeInDuration = 0.6f;

        [Header("Transição de saída")]
        [Tooltip("Image preto fullscreen com CanvasGroup. Alpha inicial = 0.")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeOutDuration = 1.0f;

        [Header("Próxima cena")]
        [SerializeField] private string nextSceneName = "Gameplay_Interno";

        // 0 = aguardando 1º input | 1 = aguardando 2º input | 2 = transicionando
        private int step;

        private void Awake()
        {
            if (captionText != null) captionText.text = captionContent;
            if (captionGroup != null) captionGroup.alpha = 0f;
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }
            step = 0;
        }

        private void Update()
        {
            if (step == 2) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool advance =
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame;

            if (!advance) return;

            if (step == 0)
            {
                StartCoroutine(FadeInCaption());
                step = 1;
            }
            else // step == 1
            {
                step = 2;
                StartCoroutine(FadeOutAndLoad());
            }
        }

        private IEnumerator FadeInCaption()
        {
            if (captionGroup == null) yield break;
            float t = 0f;
            while (t < captionFadeInDuration)
            {
                t += Time.deltaTime;
                captionGroup.alpha = Mathf.Clamp01(t / captionFadeInDuration);
                yield return null;
            }
            captionGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndLoad()
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.blocksRaycasts = true;
                float t = 0f;
                while (t < fadeOutDuration)
                {
                    t += Time.deltaTime;
                    fadeOverlay.alpha = Mathf.Clamp01(t / fadeOutDuration);
                    yield return null;
                }
                fadeOverlay.alpha = 1f;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
