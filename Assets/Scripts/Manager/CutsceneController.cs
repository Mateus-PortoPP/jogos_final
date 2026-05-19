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

        // Aviso pulsante "Pressione Enter" criado em runtime (vale p/ toda cutscene).
        private TextMeshProUGUI enterHint;

        private void CreateEnterHint()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("EnterHint");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 36f);
            rt.sizeDelta = new Vector2(900f, 60f);

            enterHint = go.AddComponent<TextMeshProUGUI>();
            enterHint.text = "▸  Pressione ENTER para continuar  ◂";
            enterHint.fontSize = 30;
            enterHint.alignment = TextAlignmentOptions.Center;
            enterHint.color = Color.white;
            enterHint.raycastTarget = false;
        }

        // Fonte das cutscenes (Jacquard 12 — SIL OFL). Criada uma vez e reusada.
        private static TMP_FontAsset cutsceneFont;

        private void ApplyCutsceneFont()
        {
            if (cutsceneFont == null)
            {
                var ttf = Resources.Load<Font>("Fonts/Jacquard12-Regular");
                if (ttf != null) cutsceneFont = TMP_FontAsset.CreateFontAsset(ttf);
            }
            if (cutsceneFont == null) return;
            // Aplica em TODOS os textos da cutscene (legenda + hint "Pressione Enter")
            foreach (var t in FindObjectsOfType<TMP_Text>(true))
                t.font = cutsceneFont;
        }

        private void Awake()
        {
            if (captionText != null) captionText.text = captionContent;
            CreateEnterHint();
            ApplyCutsceneFont(); // aplica a fonte também no hint recém-criado
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
            // Pulsa o aviso de Enter; some quando começa a transição.
            if (enterHint != null)
            {
                if (step == 2) enterHint.alpha = 0f;
                else enterHint.alpha = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
            }

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
