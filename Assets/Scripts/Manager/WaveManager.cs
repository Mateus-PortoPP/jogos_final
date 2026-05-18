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
    /// Fluxo de uma cena:
    ///   - Awake: escolhe WaveSet baseado em GameManager.CurrentNight (uma noite por cena)
    ///   - Start: contagem regressiva → primeira onda inicia sozinha
    ///   - Onda termina → InWaveUpgradePanel aparece → Enter → próxima onda (com countdown)
    ///   - Última onda → AllWavesCompleted=true → NightEndController/DoorTrigger
    ///     cuidam da transição pra próxima cena.
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
        private int enemiesReachedCastleThisWave = 0;
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
            Time.timeScale = 1f; // sai da pausa do painel de upgrade antes do countdown
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
            enemiesReachedCastleThisWave = 0;
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
            Debug.Log($"[WaveManager] Onda {CurrentWaveNumber}/{TotalWaves} iniciada.");
            StartCoroutine(SpawnWaveEnemies(wave));
        }

        private IEnumerator SpawnWaveEnemies(WaveData wave)
        {
            // Calcula o total de inimigos pra saber quando inserir o WaitForSeconds
            // entre spawns (não queremos esperar depois do último).
            int totalToSpawn = 0;
            if (wave.enemies != null)
                for (int i = 0; i < wave.enemies.Count; i++)
                    if (wave.enemies[i] != null) totalToSpawn += Mathf.Max(0, wave.enemies[i].count);

            if (totalToSpawn == 0)
            {
                Debug.LogWarning("[WaveManager] Wave sem inimigos configurados — ajuste 'enemies' no WaveSet.");
                waveSpawnFinished = true;
                if (enemiesAlive == 0) CompleteCurrentWave();
                yield break;
            }

            int spawnedSoFar = 0;
            for (int g = 0; g < wave.enemies.Count; g++)
            {
                var group = wave.enemies[g];
                if (group == null || group.prefab == null || group.count <= 0) continue;

                for (int i = 0; i < group.count; i++)
                {
                    if (spawnPoint == null)
                    {
                        Debug.LogWarning("[WaveManager] spawnPoint nulo — ajuste no Inspector.");
                        break;
                    }
                    var enemy = Instantiate(group.prefab, spawnPoint.position, Quaternion.identity);
                    enemiesAlive++;
                    OnEnemySpawned?.Invoke(enemy);
                    spawnedSoFar++;
                    if (spawnedSoFar < totalToSpawn)
                        yield return new WaitForSeconds(wave.spawnInterval);
                }
            }

            waveSpawnFinished = true;

            if (enemiesAlive == 0)
                CompleteCurrentWave();
        }

        /// <summary>Chamado quando o jogador (ou canhão) mata um inimigo.</summary>
        public void RegisterEnemyDefeated()
        {
            DecrementEnemyCount();
        }

        /// <summary>Chamado pela Fortress quando um inimigo chega no castelo. Conta como "escapado" — anula o bônus de wave limpa.</summary>
        public void RegisterEnemyReachedCastle()
        {
            enemiesReachedCastleThisWave++;
            DecrementEnemyCount();
        }

        private void DecrementEnemyCount()
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
            // Guarda contra double-complete (race entre RegisterEnemyDefeated e o
            // checkpoint final do SpawnWaveEnemies quando todos morrem antes do
            // spawn terminar).
            if (!isWaveActive) return;
            isWaveActive = false;

            // Bônus de ouro só sai se a wave foi "limpa" (nenhum inimigo escapou
            // pro castelo). Quem deixou passar inimigo perde o bônus — só ganha
            // ouro dos kills individuais.
            if (enemiesReachedCastleThisWave == 0 && goldBonusPerWave > 0)
            {
                GameManager.Instance?.AddGold(goldBonusPerWave);
                Debug.Log($"[WaveManager] Onda {CurrentWaveNumber} COMPLETA LIMPA! +{goldBonusPerWave} ouro de bônus.");
            }
            else
            {
                Debug.Log($"[WaveManager] Onda {CurrentWaveNumber} completada ({enemiesReachedCastleThisWave} escaparam pro castelo — sem bônus).");
            }

            // Set AllWavesCompleted ANTES de disparar OnWaveCompleted, pra UI
            // (InWaveUpgradePanel) saber que não deve aparecer na última onda.
            if (currentWaveIndex >= activeWaves.Count - 1)
            {
                allWavesCompleted = true;
                Debug.Log("[WaveManager] TODAS AS ONDAS DA NOITE COMPLETADAS.");
            }

            OnWaveCompleted?.Invoke(currentWaveIndex);
            if (allWavesCompleted) OnAllWavesCompleted?.Invoke();
        }
    }
}
