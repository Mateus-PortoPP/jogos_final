using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Painel UI que aparece entre as ondas pra permitir comprar upgrades.
    ///
    /// Comportamento:
    ///   - Escuta WaveManager.OnWaveCompleted → fade-in (jogador pode comprar)
    ///   - Escuta WaveManager.OnWaveStarted → fade-out
    ///   - Botões compram (gastam ouro), Enter inicia próxima onda (via WaveManager)
    ///
    /// Setup por cena:
    ///   - Gameplay_Interno: cannonEnabled = false (canhão fica com overlay "Trancado")
    ///   - Gameplay_Externo: cannonEnabled = true
    /// </summary>
    public class InWaveUpgradePanel : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private float fadeSpeed = 5f;
        [Tooltip("Se true, mostra o painel também antes da PRIMEIRA onda (logo que a cena carrega).")]
        [SerializeField] private bool showBeforeFirstWave = false;

        [Header("Custos")]
        [Tooltip("Custo do upgrade do herói no nível 0. A cada nível comprado, soma heroCostGrowth.")]
        [SerializeField] private int heroBaseCost = 30;
        [SerializeField] private int heroCostGrowth = 20;
        [Tooltip("Custo do canhão quando count=0. A cada canhão comprado, soma cannonCostGrowth.")]
        [SerializeField] private int cannonBaseCost = 50;
        [SerializeField] private int cannonCostGrowth = 25;

        [Header("Botão Herói")]
        [SerializeField] private Button heroButton;
        [SerializeField] private TextMeshProUGUI heroCostText;
        [SerializeField] private TextMeshProUGUI heroLevelText;

        [Header("Botão Canhão")]
        [SerializeField] private bool cannonEnabled = true;
        [SerializeField] private Button cannonButton;
        [SerializeField] private TextMeshProUGUI cannonCostText;
        [SerializeField] private TextMeshProUGUI cannonCountText;
        [Tooltip("Objeto que cobre o card do canhão quando trancado (ex: overlay com texto '🔒 Trancado').")]
        [SerializeField] private GameObject cannonLockedOverlay;

        [Header("Display geral")]
        [SerializeField] private TextMeshProUGUI goldText;
        [Tooltip("Texto de dica embaixo do painel — ex: 'Pressione Enter para iniciar a próxima onda'.")]
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private string hintMessage = "Pressione Enter para iniciar a próxima onda";

        private float targetAlpha;

        private void Start()
        {
            if (panelGroup != null) panelGroup.alpha = 0f;
            targetAlpha = showBeforeFirstWave ? 1f : 0f;

            if (hintText != null) hintText.text = hintMessage;

            // Bloqueia o card do canhão se a cena tem upgrade trancado.
            if (cannonLockedOverlay != null) cannonLockedOverlay.SetActive(!cannonEnabled);
            if (cannonButton != null) cannonButton.interactable = cannonEnabled;

            if (heroButton != null) heroButton.onClick.AddListener(OnBuyHero);
            if (cannonButton != null) cannonButton.onClick.AddListener(OnBuyCannon);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged += _ => RefreshUI();
                GameManager.Instance.OnPlayerUpgradeChanged += _ => RefreshUI();
                GameManager.Instance.OnCannonCountChanged += _ => RefreshUI();
            }

            if (WaveManager.Instance != null) Subscribe();
            else Invoke(nameof(Subscribe), 0.05f);

            RefreshUI();
        }

        private void Subscribe()
        {
            if (WaveManager.Instance == null) return;
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            WaveManager.Instance.OnWaveCompleted += HandleWaveCompleted;
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
                WaveManager.Instance.OnWaveCompleted -= HandleWaveCompleted;
            }
        }

        private void HandleWaveStarted(int idx) => targetAlpha = 0f;
        private void HandleWaveCompleted(int idx)
        {
            // Não mostra o painel se acabou de completar a ÚLTIMA onda da noite —
            // aí entra o fluxo de fim de noite (NightEndController ou DoorTrigger).
            var wm = WaveManager.Instance;
            if (wm != null && wm.AllWavesCompleted) return;
            targetAlpha = 1f;
        }

        private void Update()
        {
            if (panelGroup == null) return;
            panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            bool visible = panelGroup.alpha > 0.01f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }

        private int CurrentHeroCost()
        {
            int level = GameManager.Instance != null ? GameManager.Instance.PlayerUpgradeLevel : 0;
            return heroBaseCost + level * heroCostGrowth;
        }

        private int CurrentCannonCost()
        {
            int count = GameManager.Instance != null ? GameManager.Instance.CannonCount : 0;
            return cannonBaseCost + count * cannonCostGrowth;
        }

        private void OnBuyHero()
        {
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.SpendGold(CurrentHeroCost())) return;
            GameManager.Instance.AddPlayerUpgrade();
            DeselectUI();
        }

        private void OnBuyCannon()
        {
            if (!cannonEnabled) return;
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.SpendGold(CurrentCannonCost())) return;
            GameManager.Instance.AddCannon();
            DeselectUI();
        }

        // Libera o foco do EventSystem após uma compra. Sem isso, o botão recém
        // clicado fica selecionado e a próxima tecla Enter dispara Submit nele
        // em vez de iniciar a próxima onda.
        private void DeselectUI()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void RefreshUI()
        {
            int gold = GameManager.Instance != null ? GameManager.Instance.CurrentGold : 0;
            int heroLevel = GameManager.Instance != null ? GameManager.Instance.PlayerUpgradeLevel : 0;
            int cannonCount = GameManager.Instance != null ? GameManager.Instance.CannonCount : 0;
            int heroCost = CurrentHeroCost();
            int cannonCost = CurrentCannonCost();

            if (goldText != null) goldText.text = $"🪙 {gold}";
            if (heroCostText != null) heroCostText.text = $"🪙 {heroCost}";
            if (heroLevelText != null) heroLevelText.text = $"Nv {heroLevel}";
            Debug.Log($"[InWaveUpgradePanel] RefreshUI — leu GM.PlayerUpgradeLevel = {heroLevel} (GM id={(GameManager.Instance != null ? GameManager.Instance.GetInstanceID() : -1)})");
            if (cannonCostText != null) cannonCostText.text = cannonEnabled ? $"🪙 {cannonCost}" : "🔒";
            if (cannonCountText != null) cannonCountText.text = $"x {cannonCount}";

            if (heroButton != null) heroButton.interactable = gold >= heroCost;
            if (cannonButton != null) cannonButton.interactable = cannonEnabled && gold >= cannonCost;
        }
    }
}
