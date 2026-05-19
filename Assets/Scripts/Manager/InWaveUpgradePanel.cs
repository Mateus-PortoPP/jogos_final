using System.Collections;
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
        [Tooltip("Tempo (s) após o painel aparecer antes dos botões aceitarem clique. Evita clique acidental de quem estava atacando.")]
        [SerializeField] private float interactDelay = 0.7f;
        [Tooltip("Se true, mostra o painel no início da noite (depois do jogo de câmera terminar).")]
        [SerializeField] private bool showBeforeFirstWave = false;
        [Tooltip("Espera mínima (s) no início antes de checar a câmera — dá tempo do pan começar.")]
        [SerializeField] private float startShowDelay = 0.6f;

        [Header("Custos")]
        [Tooltip("Custo do upgrade do herói no nível 0. A cada nível comprado, soma heroCostGrowth.")]
        [SerializeField] private int heroBaseCost = 30;
        [SerializeField] private int heroCostGrowth = 20;
        [Tooltip("Custo da compra de CURA quando o herói está no nível MAX (ataque travado).")]
        [SerializeField] private int healCost = 40;
        [Tooltip("Custo do canhão quando count=0. A cada canhão comprado, soma cannonCostGrowth.")]
        [SerializeField] private int cannonBaseCost = 50;
        [SerializeField] private int cannonCostGrowth = 25;

        [Header("Botão Herói")]
        [SerializeField] private Button heroButton;
        [SerializeField] private TextMeshProUGUI heroCostText;
        [SerializeField] private TextMeshProUGUI heroLevelText;
        [Tooltip("Image do ícone do card Herói. Troca pro ícone de cura quando o herói atinge o nível MÁX.")]
        [SerializeField] private Image heroIconImage;
        [SerializeField] private Sprite heroNormalIcon; // upgrade_heroi
        [SerializeField] private Sprite heroHealIcon;   // upgrade_saude_heroi (modo cura no MÁX)

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
        [Tooltip("Texto exibido após a ÚLTIMA onda da noite — substitui hintMessage quando AllWavesCompleted=true.")]
        [SerializeField] private string nightEndHintMessage = "Pressione Enter para próxima noite";

        private float targetAlpha;
        private float showStartTime = -999f;
        private bool wasVisible;
        private NightEndController nightEnd;

        private void Start()
        {
            nightEnd = FindObjectOfType<NightEndController>();
            if (panelGroup != null) panelGroup.alpha = 0f;
            targetAlpha = 0f;
            // Início da noite: o painel só aparece DEPOIS do jogo de câmera
            // (intro/pan), pra não tapar a cena. Aí pausa até o jogador continuar.
            if (showBeforeFirstWave) StartCoroutine(ShowAfterCameraIntro());

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
            Time.timeScale = 1f; // segurança: nunca deixar o jogo congelado ao trocar de cena
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
                WaveManager.Instance.OnWaveCompleted -= HandleWaveCompleted;
            }
        }

        private IEnumerator ShowAfterCameraIntro()
        {
            // espera o pan começar
            yield return new WaitForSeconds(startShowDelay);
            // espera o jogo de câmera (intro/pan) terminar
            float guard = 0f;
            while (CameraFocusPan.Instance != null && CameraFocusPan.Instance.IsBusy && guard < 8f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
            // só mostra se ainda estamos antes da 1ª onda (não começou nada)
            var wm = WaveManager.Instance;
            if (wm != null && wm.IsWaveActive) yield break;
            if (hintText != null) hintText.text = hintMessage;
            targetAlpha = 1f;
            Time.timeScale = 0f; // pausa pro upgrade de início de noite
        }

        private void HandleWaveStarted(int idx)
        {
            targetAlpha = 0f;
            Time.timeScale = 1f; // retoma o jogo quando a próxima onda começa
            if (hintText != null) hintText.text = hintMessage;
        }
        private void HandleWaveCompleted(int idx)
        {
            var wm = WaveManager.Instance;
            if (wm != null && wm.AllWavesCompleted)
            {
                // Fim de noite: não congela (a transição de cena cuida disso).
                if (nightEnd != null && nightEnd.HasTargetForCurrentNight())
                {
                    if (hintText != null) hintText.text = nightEndHintMessage;
                    targetAlpha = 1f;
                }
                else
                {
                    targetAlpha = 0f; // cristal/porta — não polui a tela
                }
                return;
            }
            // Entre ondas: mostra o painel E PAUSA o jogo até o jogador continuar.
            targetAlpha = 1f;
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (panelGroup == null) return;
            // unscaled: o painel anima mesmo com o jogo pausado (timeScale 0).
            panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);

            bool visible = panelGroup.alpha > 0.01f;
            // Marca o instante em que o painel COMEÇOU a aparecer.
            if (visible && !wasVisible) showStartTime = Time.unscaledTime;
            wasVisible = visible;

            // Só aceita clique depois do grace period — quem estava martelando o
            // botão de ataque clica "no vazio" (raycast passa) em vez de comprar sem querer.
            bool acceptsInput = visible && (Time.unscaledTime - showStartTime >= interactDelay);
            panelGroup.interactable = acceptsInput;
            panelGroup.blocksRaycasts = acceptsInput;
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

            // Ataque no MAX: o botão vira compra de CURA (sem subir stats).
            if (GameManager.Instance.PlayerUpgradeMaxed)
            {
                if (!GameManager.Instance.SpendGold(healCost)) return;
                GameManager.Instance.PurchasePlayerHeal();
                DeselectUI();
                return;
            }

            // Abaixo do MAX: upgrade normal (que já cura junto).
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

            bool heroMaxed = GameManager.Instance != null && GameManager.Instance.PlayerUpgradeMaxed;

            if (goldText != null) goldText.text = $"Ouro: {gold}";
            // Ícone do herói: normal abaixo do MÁX, ícone de cura quando MÁX.
            if (heroIconImage != null)
            {
                var icon = heroMaxed ? heroHealIcon : heroNormalIcon;
                if (icon != null) heroIconImage.sprite = icon;
            }
            // No MAX o card vira "Curar" (recupera vida); abaixo do MAX é upgrade normal.
            if (heroCostText != null) heroCostText.text = heroMaxed ? $"Curar: {healCost}" : $"Ouro: {heroCost}";
            if (heroLevelText != null) heroLevelText.text = heroMaxed ? "Nv MAX" : $"Nv {heroLevel}";
            if (cannonCostText != null) cannonCostText.text = cannonEnabled ? $"Ouro: {cannonCost}" : "Travado";
            if (cannonCountText != null) cannonCountText.text = $"x {cannonCount}";

            if (heroButton != null)
                heroButton.interactable = heroMaxed ? gold >= healCost : gold >= heroCost;
            if (cannonButton != null) cannonButton.interactable = cannonEnabled && gold >= cannonCost;
        }
    }
}
