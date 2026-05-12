using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Controla as ondas de inimigos em UMA cena de gameplay.
    ///
    /// Configuração de ondas — duas opções (mutuamente exclusivas):
    ///   A) Array 'nightConfigs' (preferido): 1 WaveSet por noite. No Start, lê
    ///      GameManager.CurrentNight e usa o WaveSet correspondente. Permite
    ///      a mesma cena (Gameplay_Interno) servir N1 e N2 com ondas diferentes.
    ///   B) Lista 'waves' (legado): usada se nightConfigs estiver vazio.
    ///      Compatibilidade com a cena antiga; remova quando migrar.
    ///
    /// NÃO usa DontDestroyOnLoad — é scene-level, recriado a cada carregamento.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Configuração por noite (preferido)")]
        [Tooltip("1 WaveSet por noite. Index 0 = noite 1. Se vazio, cai pro fallback 'waves' abaixo.")]
        [SerializeField] private WaveSet[] nightConfigs;

        [Header("Configuração legada (fallback)")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;

        [Header("Recompensa")]
        [SerializeField] private int goldBonusPerWave = 25;

        [Header("Input")]
        [Tooltip("Tecla pra iniciar a próxima onda. Default: Enter.")]
        [SerializeField] private bool allowKeyboardStart = true;
        [SerializeField] private Key startWaveKey = Key.Enter;
        [SerializeField] private Key startWaveKeyAlt = Key.NumpadEnter;

        // Lista efetiva de ondas usada em runtime (vinda do nightConfigs ou do waves)
        private List<WaveData> activeWaves = new List<WaveData>();

        private int currentWaveIndex = -1;
        private int enemiesAlive = 0;
        private bool isWaveActive = false;
        private bool allWavesCompleted = false;
        private bool waveSpawnFinished = false;

        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int TotalWaves => activeWaves.Count;
        public bool IsWaveActive => isWaveActive;
        public int EnemiesAlive => enemiesAlive;
        public bool AllWavesCompleted => allWavesCompleted;
        public bool CanStartNextWave => !isWaveActive && !allWavesCompleted && activeWaves.Count > 0;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnAllWavesCompleted;
        public event Action<GameObject> OnEnemySpawned;
        public event Action OnEnemyDefeated;

        private void Awake()
        {
            // Scene-level singleton (sem DontDestroyOnLoad).
            // Se houver duplicata na mesma cena, destrói a nova.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveActiveWaves();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Escolhe qual lista de ondas usar:
        /// - Se nightConfigs tem entradas válidas, usa o índice correspondente à noite atual.
        /// - Senão, usa o fallback 'waves'.
        /// </summary>
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

            // Garante que o GameManager comece a noite zerado.
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

            currentWaveIndex++;
            if (currentWaveIndex >= activeWaves.Count)
            {
                currentWaveIndex = activeWaves.Count - 1;
                return;
            }

            GameManager.Instance?.StartNewWave(activeWaves.Count);

            isWaveActive = true;
            waveSpawnFinished = false;
            enemiesAlive = 0;

            var wave = activeWaves[currentWaveIndex];
            Debug.Log($"[WaveManager] Onda {CurrentWaveNumber}/{TotalWaves} iniciada — {wave.enemyCount} inimigos.");

            OnWaveStarted?.Invoke(currentWaveIndex);
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
            OnWaveCompleted?.Invoke(currentWaveIndex);

            if (currentWaveIndex >= activeWaves.Count - 1)
            {
                allWavesCompleted = true;
                Debug.Log($"[WaveManager] TODAS AS ONDAS DA NOITE COMPLETADAS.");
                OnAllWavesCompleted?.Invoke();
            }
        }
    }
}
