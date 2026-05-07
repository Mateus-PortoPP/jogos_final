using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Singleton que controla as ondas de inimigos.
    ///
    /// - O jogador inicia a próxima onda manualmente (tecla R por enquanto).
    /// - Os inimigos da onda spawnam escalonados no tempo (intervalo configurável).
    /// - A onda só é considerada "completa" quando todos foram spawnados E todos
    ///   foram derrotados (ou chegaram no castelo — o Goblin chama
    ///   RegisterEnemyDefeated nos dois casos).
    /// - Ao completar onda: bônus de ouro pelo GameManager.
    /// - Após a última onda: dispara TriggerVictory no GameManager.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Configuração das Ondas")]
        [Tooltip("Lista das ondas. Configure size = 5 e ajuste cada item.")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();

        [Header("Spawn")]
        [Tooltip("Onde os inimigos aparecem (geralmente um Empty na borda direita do mapa).")]
        [SerializeField] private Transform spawnPoint;

        [Header("Recompensa")]
        [Tooltip("Ouro extra dado ao completar uma onda inteira.")]
        [SerializeField] private int goldBonusPerWave = 25;

        [Header("Input")]
        [Tooltip("Tecla pra iniciar a próxima onda. Temporário — depois trocamos por botão de UI.")]
        [SerializeField] private Key startWaveKey = Key.R;

        // -1 = ainda não começou nenhuma onda. 0 = primeira onda em andamento, etc.
        private int currentWaveIndex = -1;
        private int enemiesAlive = 0;
        private bool isWaveActive = false;
        private bool allWavesCompleted = false;
        // Marca quando o spawn da onda atual terminou (todos foram instanciados).
        // Sem isso, se um goblin morrer ANTES do último spawnar, completaríamos a
        // onda cedo demais.
        private bool waveSpawnFinished = false;

        // Properties só-leitura pra UI / outros sistemas consultarem o estado.
        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int TotalWaves => waves.Count;
        public bool IsWaveActive => isWaveActive;
        public int EnemiesAlive => enemiesAlive;
        public bool AllWavesCompleted => allWavesCompleted;
        public bool CanStartNextWave => !isWaveActive && !allWavesCompleted;

        // Eventos: outros sistemas (HUD, sons, etc.) se inscrevem aqui pra reagir.
        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnAllWavesCompleted;
        public event Action<GameObject> OnEnemySpawned;
        public event Action OnEnemyDefeated;

        private void Awake()
        {
            // Padrão singleton igual ao GameManager — duplicata se autodestrói.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Tecla pra iniciar a próxima onda (versão temporária do "botão Iniciar Onda")
            // Usa o Input System novo (Keyboard.current), porque o projeto está nessa configuração.
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard[startWaveKey].wasPressedThisFrame && CanStartNextWave)
            {
                StartNextWave();
            }
        }

        /// <summary>
        /// Inicia a próxima onda da lista. Não faz nada se já estiver em andamento
        /// ou se todas as ondas já foram completadas.
        /// </summary>
        public void StartNextWave()
        {
            if (!CanStartNextWave) return;

            currentWaveIndex++;
            if (currentWaveIndex >= waves.Count)
            {
                // Defesa adicional — não deveria acontecer porque CanStartNextWave já checa
                currentWaveIndex = waves.Count - 1;
                return;
            }

            // Sincroniza com o GameManager (atualiza contador global de wave)
            GameManager.Instance?.StartNewWave();

            isWaveActive = true;
            waveSpawnFinished = false;
            enemiesAlive = 0;

            var wave = waves[currentWaveIndex];
            Debug.Log($"[WaveManager] Onda {CurrentWaveNumber} iniciada — {wave.enemyCount} inimigos, intervalo {wave.spawnInterval}s.");

            OnWaveStarted?.Invoke(currentWaveIndex);
            StartCoroutine(SpawnWaveEnemies(wave));
        }

        /// <summary>
        /// Corrotina que spawna os inimigos da onda atual, escalonados no tempo.
        /// </summary>
        private IEnumerator SpawnWaveEnemies(WaveData wave)
        {
            for (int i = 0; i < wave.enemyCount; i++)
            {
                if (wave.enemyPrefab != null && spawnPoint != null)
                {
                    var enemy = Instantiate(wave.enemyPrefab, spawnPoint.position, Quaternion.identity);
                    enemiesAlive++;
                    Debug.Log($"[WaveManager] Inimigo spawnado ({enemiesAlive} vivos).");
                    OnEnemySpawned?.Invoke(enemy);
                }
                else
                {
                    Debug.LogWarning("[WaveManager] enemyPrefab ou spawnPoint nulo — ajuste no Inspector.");
                }

                // Espera o intervalo antes de spawnar o próximo (exceto após o último)
                if (i < wave.enemyCount - 1)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }

            waveSpawnFinished = true;

            // Caso raro: se TODOS já morreram antes do último spawnar (intervalo grande
            // + jogador OP), completa a onda agora.
            if (enemiesAlive == 0)
                CompleteCurrentWave();
        }

        /// <summary>
        /// Chamado pelo Goblin (e qualquer outro inimigo) ao morrer ou chegar no castelo.
        /// </summary>
        public void RegisterEnemyDefeated()
        {
            enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
            Debug.Log($"[WaveManager] Inimigo derrotado ({enemiesAlive} vivos).");
            OnEnemyDefeated?.Invoke();

            // Onda só termina se todos já foram spawnados E todos foram derrotados.
            if (isWaveActive && waveSpawnFinished && enemiesAlive == 0)
            {
                CompleteCurrentWave();
            }
        }

        /// <summary>
        /// Finaliza a onda atual: dá bônus, dispara eventos. Se era a última,
        /// marca AllWavesCompleted e chama TriggerVictory no GameManager.
        /// </summary>
        private void CompleteCurrentWave()
        {
            isWaveActive = false;

            GameManager.Instance?.AddGold(goldBonusPerWave);
            Debug.Log($"[WaveManager] Onda {CurrentWaveNumber} completada! +{goldBonusPerWave} ouro de bônus.");
            OnWaveCompleted?.Invoke(currentWaveIndex);

            // Se era a última onda, vitória total
            if (currentWaveIndex >= waves.Count - 1)
            {
                allWavesCompleted = true;
                Debug.Log("[WaveManager] TODAS AS ONDAS COMPLETADAS — VITÓRIA.");
                OnAllWavesCompleted?.Invoke();
                GameManager.Instance?.TriggerVictory();
            }
        }
    }
}
