using System.Collections;
using TMPro;
using UnityEngine;

namespace TowerDefense.Manager
{
    public class BossAlertHUD : MonoBehaviour
    {
        public static BossAlertHUD Instance { get; private set; }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private string text = "O CHEFE CHEGOU";
        [SerializeField] private float fadeIn = 0.4f;
        [SerializeField] private float hold = 1.8f;
        [SerializeField] private float fadeOut = 0.6f;

        private Coroutine routine;

        private void Awake()
        {
            Instance = this;
            if (group != null) group.alpha = 0f;
            if (label != null) label.text = text;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowAlert()
        {
            if (group == null) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            float t = 0f;
            while (t < fadeIn) { t += Time.deltaTime; group.alpha = Mathf.Clamp01(t / fadeIn); yield return null; }
            group.alpha = 1f;
            yield return new WaitForSeconds(hold);
            t = 0f;
            while (t < fadeOut) { t += Time.deltaTime; group.alpha = 1f - Mathf.Clamp01(t / fadeOut); yield return null; }
            group.alpha = 0f;
            routine = null;
        }
    }
}
