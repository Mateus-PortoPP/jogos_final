using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Singleton de fade entre cenas. Auto-cria um Canvas overlay com uma Image
    /// preta na primeira vez que alguém usa SceneFader.Instance — então nenhum
    /// setup manual é necessário no Inspector.
    ///
    /// Uso:
    ///   SceneFader.Instance.FadeOutAndLoad("Cutscene_Noite3");
    ///   SceneFader.Instance.FadeIn();   // chamado automaticamente após load
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [SerializeField] private float defaultFadeDuration = 0.8f;
        [SerializeField] private Color fadeColor = Color.black;

        private CanvasGroup canvasGroup;
        private Image image;
        private bool fading;

        public static SceneFader GetOrCreate()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("[SceneFader]");
            var fader = go.AddComponent<SceneFader>();
            return fader;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // sempre por cima

            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var imgGO = new GameObject("FadeImage");
            imgGO.transform.SetParent(canvasGO.transform, false);

            image = imgGO.AddComponent<Image>();
            image.color = fadeColor;
            image.raycastTarget = false;

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            canvasGroup = imgGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>Fade-out (escurece a tela), carrega a cena, depois fade-in automático.</summary>
        public void FadeOutAndLoad(string sceneName, float duration = -1f)
        {
            if (fading) return;
            if (duration < 0f) duration = defaultFadeDuration;
            StartCoroutine(FadeOutAndLoadRoutine(sceneName, duration));
        }

        public void FadeIn(float duration = -1f)
        {
            if (duration < 0f) duration = defaultFadeDuration;
            StartCoroutine(FadeRoutine(1f, 0f, duration));
        }

        private IEnumerator FadeOutAndLoadRoutine(string sceneName, float duration)
        {
            fading = true;
            canvasGroup.blocksRaycasts = true;
            yield return FadeRoutine(0f, 1f, duration);
            SceneManager.LoadScene(sceneName);
            // dá 1 frame pra cena carregar
            yield return null;
            yield return FadeRoutine(1f, 0f, duration);
            canvasGroup.blocksRaycasts = false;
            fading = false;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            float t = 0f;
            canvasGroup.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
