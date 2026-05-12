using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Porta-gatilho que leva à próxima cena depois que todas as ondas da noite atual
    /// foram completadas. Default: ativa apenas na noite 2 (final da fase interna).
    ///
    /// Setup na cena:
    ///   - GameObject com Sprite Renderer + BoxCollider2D (Is Trigger = true)
    ///   - Tag do player precisa bater com 'playerTag' (default 'Player')
    ///   - 'requiredNight' = noite em que a porta deve abrir (default 2)
    ///   - 'nextScene' = cena pra carregar (default 'Cutscene_Noite3')
    ///   - Opcional: GameObject filho com a seta flutuante (referência em 'doorIndicator')
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DoorTrigger : MonoBehaviour
    {
        [Header("Condição")]
        [SerializeField] private int requiredNight = 2;
        [SerializeField] private string playerTag = "Player";

        [Header("Transição")]
        [SerializeField] private string nextScene = "Cutscene_Externa";

        [Header("Visual")]
        [Tooltip("GameObject filho ativado quando a porta libera (ex: seta flutuante).")]
        [SerializeField] private GameObject doorIndicator;
        [Tooltip("Cor do sprite quando ativa. Deixe Color.white pra não alterar.")]
        [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private SpriteRenderer doorSprite;

        private Collider2D col;
        private bool isActive;
        private bool triggered;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            col.enabled = false;
            if (doorIndicator != null) doorIndicator.SetActive(false);
            if (doorSprite == null) doorSprite = GetComponent<SpriteRenderer>();
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
            int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 0;
            if (night != requiredNight) return;
            Activate();
        }

        private void Activate()
        {
            if (isActive) return;
            isActive = true;
            col.enabled = true;
            if (doorIndicator != null) doorIndicator.SetActive(true);
            if (doorSprite != null) doorSprite.color = activeColor;
            Debug.Log($"[DoorTrigger] Porta ativa — vá até ela pra ir para '{nextScene}'.");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || !isActive) return;
            if (!other.CompareTag(playerTag)) return;
            triggered = true;
            GameManager.Instance?.AdvanceNight();
            Debug.Log($"[DoorTrigger] Player entrou na porta — avançando noite e carregando '{nextScene}'.");
            SceneFader.GetOrCreate().FadeOutAndLoad(nextScene);
        }
    }
}
