using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Controla as ondas de inimigos em UMA cena de gameplay.
    ///
    /// Fluxo de uma cena (ex: Gameplay_Interno com N1 e N2):
    ///   - Awake: escolhe WaveSet baseado em GameManager.CurrentNight
    ///   - Start: contagem regressiva → primeira onda inicia sozinha
    ///   - Onda termina → InWaveUpgradePanel aparece → jogador aperta Enter → próxima onda (com countdown)
    ///   - Última onda da noite atual + existe próxima noite no mesmo nightConfigs?
    ///       → Auto-advance: chama GameManager.AdvanceNight() e troca activeWaves pelo WaveSet da próxima noite.
    ///         O painel ainda aparece, jogador pressiona Enter, continua jogando.
    ///   - Última onda da última noite desta cena → AllWavesCompleted=true → NightEndController/DoorTrigger.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Configuração por noite (preferido)")]
        [Tooltip("1 WaveSet por noite. Index 0 = noite 1. Slots vazios significam 'esta noite não acontece nesta cena' — útil pra ter N1+N2 em Interno e N3+N4 em Externo.")]
        [SerializeField] private WaveSet[] nightConfigs;

        [Header("Configuração legada (fallback)")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;

        [Header("Recompensa")]
        [SerializeField] private int goldBonusPerWave = 25;

        [Header("Input")]
        [SerializeField] private bool allowKeyboardStart = true;
        [SerializeField] private Key startWaveKey = Key.Enter;
        [SerializeField] private Key startWaveKeyAlt = Key.NumpadEnter;

        [Header("Countdown")]
        [Tooltip("Segundos de contagem antes de cada onda spawnar ('3, 2, 1, Vai!'). 0 desliga a contagem.")]
        [SerializeField] private float preWaveCountdown = 3f;
        [Tooltip("TextMeshProUGUI centralizado pra exibir a contagem. Some quando a onda começa a spawnar.")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [Tooltip("Se true, a PRIMEIRA onda da cena dispara sozinha (após a contagem). As seguintes exigem Enter no painel.")]
        [SerializeField] private bool autoStartFirstWave = true;

        // Lista efetiva de ondas usada em runtime (vinda do nightConfigs ou do waves)
        private List<WaveData> activeWaves = new List<WaveData>();

        private int currentWaveIndex = -1;
        private int enemiesAlive = 0;
        private bool isWaveActive = false;
        private bool allWavesCompleted = false;
        private bool waveSpawnFinished = false;
        private bool isCountingDown = false;

        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int TotalWaves => activeWaves.Count;
        public bool IsWaveActive => isWaveActive;
        public int EnemiesAlive => enemiesAlive;
        public bool AllWavesCompleted => allWavesCompleted;
        public bool CanStartNextWave => !isWaveActive && !allWavesCompleted && !isCountingDown && activeWaves.Count > 0;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnAllWavesCompleted;
        public event Action<GameObject> OnEnemySpawned;
        public event Action OnEnemyDefeated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ResolveActiveWaves();
        }

        private void Start()
        {
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (autoStartFirstWave && activeWaves.Count > 0)
            {
                StartNextWave();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void ResolveActiveWaves()
        {
            int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1;
            int idx = Mathf.Clamp(night - 1, 0, (nightConfigs != null ? nightConfigs.Length : 0) - 1);

            if (nightConfigs != null && nightConfigs.Length > 0 && nightConfigs[idx] != null)
            {
                activeWaves = new List<WaveData>(nightConfigs[idx].waves);
                Debug.Log($"[WaveManager] Usando WaveSet '{nightConfigs[idx].name}' ({activeWaves.Count} ondas) pra noite {night}.");
            }
            else
            {
                activeWaves = waves;
                Debug.Log($"[WaveManager] Usando fallback 'waves' ({activeWaves.Count} ondas).");
            }

            GameManager.Instance?.ResetWaveCounter();
        }

        private void Update()
        {
            if (!allowKeyboardStart) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool pressed = keyboard[startWaveKey].wasPressedThisFrame
                        || keyboard[startWaveKeyAlt].wasPressedThisFrame;
            if (pressed && CanStartNextWave)
            {
                StartNextWave();
            }
        }

        public void StartNextWave()
        {
            if (!CanStartNextWave) return;
            StartCoroutine(CountdownThenSpawnNextWave());
        }

        private IEnumerator CountdownThenSpawnNextWave()
        {
            isCountingDown = true;
            int nextIdx = currentWaveIndex + 1;
            if (nextIdx >= activeWaves.Count)
            {
                isCountingDown = false;
                yield break;
            }

            // Avança o índice e fire OnWaveStarted ANTES da contagem, pra a UI
            // (painel de upgrade) saber que a onda começou e dar fade-out enquanto
            // o countdown rola.
            currentWaveIndex = nextIdx;
            isWaveActive = true;
            waveSpawnFinished = false;
            enemiesAlive = 0;
            GameManager.Instance?.StartNewWave(activeWaves.Count);
            OnWaveStarted?.Invoke(currentWaveIndex);

            if (preWaveCountdown > 0f)
            {
                if (countdownText != null) countdownText.gameObject.SetActive(true);
                float t = preWaveCountdown;
                while (t > 0f)
                {
                    if (countdownText != null) countdownText.text = Mathf.CeilToInt(t).ToString();
                    yield return null;
                    t -= Time.deltaTime;
                }
                if (countdownText != null)
                {
                    countdownText.text = "Vai!";
                    yield return new WaitForSeconds(0.4f);
                    countdownText.gameObject.SetActive(false);
                }
            }
            isCountingDown = false;

            var wave = activeWaves[currentWaveIndex];
            Debug.Log($"[WaveManager] Onda {CurrentWaveNumber}/{TotalWaves} iniciada — {wave.enemyCount} inimigos.");
            StartCoroutine(SpawnWaveEnemies(wave));
        }

        private IEnumerator SpawnWaveEnemies(WaveData wave)
        {
            for (int i = 0; i < wave.enemyCount; i++)
            {
                if (wave.enemyPrefab != null && spawnPoint != null)
                {
                    var enemy = Instantiate(wave.enemyPrefab, spawnPoint.position, Quaternion.identity);
                    enemiesAlive++;
                    OnEnemySpawned?.Invoke(enemy);
                }
                else
                {
                    Debug.LogWarning("[WaveManager] enemyPrefab ou spawnPoint nulo — ajuste no Inspector.");
                }

                if (i < wave.enemyCount - 1)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }

            waveSpawnFinished = true;

            if (enemiesAlive == 0)
                CompleteCurrentWave();
        }

        public void RegisterEnemyDefeated()
        {
            enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
            OnEnemyDefeated?.Invoke();

            if (isWaveActive && waveSpawnFinished && enemiesAlive == 0)
            {
                CompleteCurrentWave();
            }
        }

        private void CompleteCurrentWave()
        {
            isWaveActive = false;
            GameManager.Instance?.AddGold(goldBonusPerWave);
            Debug.Log($"[WaveManager] Onda {CurrentWaveNumber} completada! +{goldBonusPerWave} ouro.");

            int completedIdx = currentWaveIndex;
            bool isLastOfNight = currentWaveIndex >= activeWaves.Count - 1;

            if (isLastOfNight)
            {
                // Próxima noite configurada nesta cena? Auto-advance pra continuar
                // no mesmo cenário sem trocar de cena.
                int curNight = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1;
                int nextNightIdx = curNight; // 0-based pra noite seguinte (curNight é 1-based)
                bool hasNextNightInScene = nightConfigs != null
                    && nextNightIdx >= 0 && nextNightIdx < nightConfigs.Length
                    && nightConfigs[nextNightIdx] != null;

                if (hasNextNightInScene)
                {
                    GameManager.Instance?.AdvanceNight();
                    activeWaves = new List<WaveData>(nightConfigs[nextNightIdx].waves);
                    currentWaveIndex = -1;
                    waveSpawnFinished = false;
                    Debug.Log($"[WaveManager] Auto-advance pra noite {curNight + 1} ({activeWaves.Count} ondas).");
                    OnWaveCompleted?.Invoke(completedIdx);
                    return;
                }

                // Última noite desta cena: marca como completed ANTES de disparar
                // OnWaveCompleted, pra UI (painel) saber que não deve aparecer.
                allWavesCompleted = true;
                Debug.Log("[WaveManager] TODAS AS ONDAS DA NOITE COMPLETADAS.");
            }

            OnWaveCompleted?.Invoke(completedIdx);
            if (allWavesCompleted) OnAllWavesCompleted?.Invoke();
        }
    }
}
