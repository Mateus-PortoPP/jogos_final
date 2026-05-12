using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Controla a cena UpgradeSelect.
    ///
    /// Fluxo:
    ///   - Player chega → vê ouro disponível e duas opções (Canhão / Upgrade Cavaleiro)
    ///   - Clica em uma (ou nas duas, ou nenhuma) → ouro é gasto, upgrade contabilizado
    ///   - Pressiona ENTER → AdvanceNight + carrega próxima cena
    /// </summary>
    public class UpgradeSelectController : MonoBehaviour
    {
        [Header("Custos")]
        [SerializeField] private int cannonCost = 50;
        [SerializeField] private int playerUpgradeCost = 75;

        [Header("UI — Canhão")]
        [SerializeField] private Button cannonButton;
        [SerializeField] private TextMeshProUGUI cannonCostText;
        [SerializeField] private TextMeshProUGUI cannonOwnedText;

        [Header("UI — Cavaleiro")]
        [SerializeField] private Button playerUpgradeButton;
        [SerializeField] private TextMeshProUGUI playerUpgradeCostText;
        [SerializeField] private TextMeshProUGUI playerUpgradeLevelText;

        [Header("UI — Geral")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Button skipButton;

        [Header("Fluxo")]
        [Tooltip("Cena para carregar após o jogador escolher (ou pular).")]
        [SerializeField] private string nextSceneAfterChoice = "Gameplay_Interno";
        [Tooltip("Se true, avança a noite (GameManager.AdvanceNight) antes de carregar a próxima cena.")]
        [SerializeField] private bool advanceNightOnExit = true;

        private void Start()
        {
            if (cannonCostText != null) cannonCostText.text = $"🪙 {cannonCost}";
            if (playerUpgradeCostText != null) playerUpgradeCostText.text = $"🪙 {playerUpgradeCost}";

            if (cannonButton != null) cannonButton.onClick.AddListener(OnBuyCannon);
            if (playerUpgradeButton != null) playerUpgradeButton.onClick.AddListener(OnBuyPlayerUpgrade);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged += UpdateGoldUI;
                GameManager.Instance.OnCannonCountChanged += UpdateCannonOwned;
                GameManager.Instance.OnPlayerUpgradeChanged += UpdatePlayerUpgradeLevel;
            }

            UpdateGoldUI(GameManager.Instance != null ? GameManager.Instance.CurrentGold : 0);
            UpdateCannonOwned(GameManager.Instance != null ? GameManager.Instance.CannonCount : 0);
            UpdatePlayerUpgradeLevel(GameManager.Instance != null ? GameManager.Instance.PlayerUpgradeLevel : 0);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged -= UpdateGoldUI;
                GameManager.Instance.OnCannonCountChanged -= UpdateCannonOwned;
                GameManager.Instance.OnPlayerUpgradeChanged -= UpdatePlayerUpgradeLevel;
            }
        }

        private void OnBuyCannon()
        {
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.SpendGold(cannonCost)) return;
            GameManager.Instance.AddCannon();
        }

        private void OnBuyPlayerUpgrade()
        {
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.SpendGold(playerUpgradeCost)) return;
            GameManager.Instance.AddPlayerUpgrade();
        }

        private void OnSkip() => Proceed();

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                Proceed();
            }
        }

        private void Proceed()
        {
            if (advanceNightOnExit) GameManager.Instance?.AdvanceNight();
            SceneFader.GetOrCreate().FadeOutAndLoad(nextSceneAfterChoice);
        }

        private void UpdateGoldUI(int gold)
        {
            if (goldText != null) goldText.text = $"🪙 {gold}";
            if (cannonButton != null) cannonButton.interactable = gold >= cannonCost;
            if (playerUpgradeButton != null) playerUpgradeButton.interactable = gold >= playerUpgradeCost;
        }

        private void UpdateCannonOwned(int count)
        {
            if (cannonOwnedText != null) cannonOwnedText.text = $"Canhões: {count}";
        }

        private void UpdatePlayerUpgradeLevel(int level)
        {
            if (playerUpgradeLevelText != null) playerUpgradeLevelText.text = $"Nível: {level}";
        }
    }
}
