using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Singleton global que controla o estado macro do jogo:
    /// ouro, HP da fortaleza, noite atual, upgrades comprados, e flags de fim.
    /// Persiste entre cenas (DontDestroyOnLoad) — a UI se inscreve nos eventos.
    /// </summary>
    // Roda o Awake ANTES do WaveManager (que lê CurrentNight pra resolver as ondas).
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Economia")]
        [SerializeField] private int startingGold = 0;

        [Header("Fortaleza")]
        [SerializeField] private int maxFortressHP = 60;

        [Header("Noites")]
        [SerializeField] private int totalNights = 5;

        [Header("Upgrade do cavaleiro")]
        [Tooltip("Nível máximo do upgrade do herói. Impede o player de virar invencível e matar o boss em 2 golpes.")]
        [SerializeField] private int maxPlayerUpgradeLevel = 8;

        [Header("DEBUG — loadout de teste (NÃO ligar no build final)")]
        [Tooltip("Se ligado, ao dar Play DIRETO nesta cena o jogo começa já com noite/ouro/poderes definidos. Inerte numa run completa (o GameManager persistente vence).")]
        [SerializeField] private bool debugLoadout = false;
        [SerializeField] private int debugNight = 5;
        [SerializeField] private int debugGold = 500;
        [SerializeField] private bool debugStellarUnlocked = true;
        [SerializeField] private int debugCannonCount = 3;
        [SerializeField] private int debugPlayerUpgradeLevel = 4;

        // --- Estado mutável ---
        private int currentGold;
        private int currentFortressHP;
        private int currentWave;       // wave dentro da noite atual
        private int currentNight;      // 1..totalNights
        private int cannonCount;       // canhões comprados (aplicados na próxima cena de gameplay)
        private int playerUpgradeLevel;// nível acumulado de upgrade do cavaleiro
        private bool stellarPowersUnlocked; // Corte Estelar (LMB hold) + Investida (Q) — liberados após o Cristal Estelar
        private bool isGameOver;
        private bool isVictory;

        // --- Properties read-only ---
        public int CurrentGold => currentGold;
        public int CurrentFortressHP => currentFortressHP;
        public int MaxFortressHP => maxFortressHP;
        public int CurrentWave => currentWave;
        public int CurrentNight => currentNight;
        public int TotalNights => totalNights;
        public int CannonCount => cannonCount;
        public int PlayerUpgradeLevel => playerUpgradeLevel;
        public int MaxPlayerUpgradeLevel => maxPlayerUpgradeLevel;
        public bool PlayerUpgradeMaxed => playerUpgradeLevel >= maxPlayerUpgradeLevel;
        public bool IsStellarPowersUnlocked => stellarPowersUnlocked;
        public bool IsGameOver => isGameOver;
        public bool IsVictory => isVictory;

        // --- Eventos ---
        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnFortressHPChanged;
        public event Action<int, int> OnWaveChanged;       // (waveIndex, totalWavesNaNoite)
        public event Action<int, int> OnNightChanged;      // (currentNight, totalNights)
        public event Action<int> OnCannonCountChanged;
        public event Action<int> OnPlayerUpgradeChanged;
        public event Action OnPlayerHealPurchased;   // compra de cura (pós nível MAX)
        public event Action OnStellarPowersUnlocked;
        public event Action OnGameOver;
        public event Action OnVictory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResetGame();

            if (debugLoadout) ApplyDebugLoadout();
        }

        private void ApplyDebugLoadout()
        {
            currentNight = Mathf.Clamp(debugNight, 1, totalNights);
            currentGold = Mathf.Max(0, debugGold);
            cannonCount = Mathf.Max(0, debugCannonCount);
            playerUpgradeLevel = Mathf.Clamp(debugPlayerUpgradeLevel, 0, maxPlayerUpgradeLevel);
            stellarPowersUnlocked = debugStellarUnlocked;

            OnGoldChanged?.Invoke(currentGold);
            OnNightChanged?.Invoke(currentNight, totalNights);
            OnCannonCountChanged?.Invoke(cannonCount);
            OnPlayerUpgradeChanged?.Invoke(playerUpgradeLevel);
            if (stellarPowersUnlocked) OnStellarPowersUnlocked?.Invoke();

            Debug.Log("[GameManager] DEBUG loadout: noite " + currentNight + ", ouro " + currentGold
                + ", canhoes " + cannonCount + ", upgrade " + playerUpgradeLevel
                + ", estelar " + stellarPowersUnlocked);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }

        public bool SpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (currentGold < amount) return false;
            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }

        public void TakeFortressDamage(int amount)
        {
            if (isGameOver || amount <= 0) return;
            currentFortressHP = Mathf.Max(0, currentFortressHP - amount);
            OnFortressHPChanged?.Invoke(currentFortressHP, maxFortressHP);

            if (currentFortressHP == 0)
            {
                TriggerGameOver();
            }
        }

        /// <summary>
        /// WaveManager chama isso ao iniciar cada onda DENTRO de uma noite.
        /// Reseta o contador no início da cena de gameplay quando muda de noite.
        /// </summary>
        public void StartNewWave(int wavesPerNight)
        {
            currentWave++;
            OnWaveChanged?.Invoke(currentWave, wavesPerNight);
        }

        /// <summary>Overload mantido por compat — usado pelo WaveManager atual até a task #18.</summary>
        public void StartNewWave() => StartNewWave(0);

        /// <summary>
        /// Avança para a próxima noite. Reseta contador de ondas.
        /// Chamado pelo fluxo de cenas (após upgrade ou após cutscene).
        /// </summary>
        public void AdvanceNight()
        {
            currentNight++;
            currentWave = 0;
            OnNightChanged?.Invoke(currentNight, totalNights);
        }

        /// <summary>
        /// Reseta o contador de ondas (chamado pelo WaveManager no Start
        /// se a cena foi recarregada para uma noite nova).
        /// </summary>
        public void ResetWaveCounter()
        {
            currentWave = 0;
        }

        public void AddCannon()
        {
            cannonCount++;
            OnCannonCountChanged?.Invoke(cannonCount);
        }

        public void AddPlayerUpgrade()
        {
            if (playerUpgradeLevel >= maxPlayerUpgradeLevel) return; // teto: não vira invencível
            playerUpgradeLevel++;
            OnPlayerUpgradeChanged?.Invoke(playerUpgradeLevel);
        }

        /// <summary>Compra de cura — disponível mesmo com o ataque no MAX. Não altera stats.</summary>
        public void PurchasePlayerHeal()
        {
            OnPlayerHealPurchased?.Invoke();
        }

        /// <summary>
        /// Desbloqueia o Corte Estelar (LMB hold) e a Investida (Q). Chamado quando
        /// o jogador toca o Cristal Estelar entre N2 e N3. Idempotente.
        /// </summary>
        public void UnlockStellarPowers()
        {
            if (stellarPowersUnlocked) return;
            stellarPowersUnlocked = true;
            Debug.Log("[GameManager] Poderes estelares DESBLOQUEADOS — Corte Estelar (LMB hold) e Investida (Q) ativos.");
            OnStellarPowersUnlocked?.Invoke();
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;
            OnGameOver?.Invoke();
            SceneManager.LoadScene("GameOver");
        }

        public void TriggerVictory()
        {
            if (isVictory || isGameOver) return;
            isVictory = true;
            OnVictory?.Invoke();
        }

        /// <summary>
        /// Reset completo — usado pelo botão "Tentar de novo" do Game Over
        /// ou ao voltar do menu inicial.
        /// </summary>
        public void ResetGame()
        {
            currentGold = startingGold;
            currentFortressHP = maxFortressHP;
            currentWave = 0;
            currentNight = 1;
            cannonCount = 0;
            playerUpgradeLevel = 0;
            stellarPowersUnlocked = false;
            isGameOver = false;
            isVictory = false;

            OnGoldChanged?.Invoke(currentGold);
            OnFortressHPChanged?.Invoke(currentFortressHP, maxFortressHP);
            OnWaveChanged?.Invoke(currentWave, 0);
            OnNightChanged?.Invoke(currentNight, totalNights);
            OnCannonCountChanged?.Invoke(cannonCount);
            OnPlayerUpgradeChanged?.Invoke(playerUpgradeLevel);
        }
    }
}
