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
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Economia")]
        [SerializeField] private int startingGold = 0;

        [Header("Fortaleza")]
        [SerializeField] private int maxFortressHP = 60;

        [Header("Noites")]
        [SerializeField] private int totalNights = 4;

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
            playerUpgradeLevel++;
            Debug.Log($"[GameManager] AddPlayerUpgrade chamado — novo level = {playerUpgradeLevel} (GM id={GetInstanceID()})");
            OnPlayerUpgradeChanged?.Invoke(playerUpgradeLevel);
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
