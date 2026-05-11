using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Singleton global que controla o estado macro do jogo:
    /// ouro do jogador, HP da fortaleza, waves, e flags de fim de jogo.
    ///
    /// Por que singleton:
    /// - Existe apenas UM estado de jogo, e qualquer script (HUD, Goblin,
    ///   Fortaleza, Tower) precisa ler/escrever nele. O singleton expõe
    ///   GameManager.Instance pra facilitar esse acesso sem precisar arrastar
    ///   referência manualmente em cada Inspector.
    ///
    /// Por que DontDestroyOnLoad:
    /// - Nas próximas sprints vamos ter Menu → Game → Game Over como cenas
    ///   diferentes. Persistir o GameManager entre cenas evita reinicializar
    ///   por acidente e simplifica a lógica de "voltar ao menu sem perder estado".
    ///
    /// Por que eventos (System.Action):
    /// - Pra desacoplar a UI (HUDManager) e outros sistemas do GameManager.
    ///   O HUD se inscreve em OnGoldChanged e atualiza o texto sozinho —
    ///   o GameManager nem sabe que existe um HUD. Isso permite trocar a UI
    ///   sem mexer em lógica de jogo, e adicionar novos ouvintes (sons,
    ///   achievements, etc) sem alterar este script.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Economia")]
        [SerializeField] private int startingGold = 150;

        [Header("Fortaleza")]
        [SerializeField] private int maxFortressHP = 60;

        [Header("Waves")]
        [SerializeField] private int totalWaves = 5;

        // Estado mutável — privado, exposto via property somente leitura.
        private int currentGold;
        private int currentFortressHP;
        private int currentWave;
        private bool isGameOver;
        private bool isVictory;

        // Properties pra leitura externa (não permitem escrita direta de fora).
        public int CurrentGold => currentGold;
        public int CurrentFortressHP => currentFortressHP;
        public int MaxFortressHP => maxFortressHP;
        public int CurrentWave => currentWave;
        public int TotalWaves => totalWaves;
        public bool IsGameOver => isGameOver;
        public bool IsVictory => isVictory;

        // Eventos: outros scripts se inscrevem pra reagir a mudanças.
        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnFortressHPChanged;
        public event Action<int, int> OnWaveChanged;
        public event Action OnGameOver;
        public event Action OnVictory;

        private void Awake()
        {
            // Garante que existe apenas uma instância. Se já houver outra,
            // este GameObject duplicado se autodestrói (acontece quando se
            // volta pra cena que tem o GameManager pré-colocado).
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResetGame();
        }

        /// <summary>
        /// Adiciona ouro ao jogador (ex: por matar inimigo, completar wave).
        /// Dispara OnGoldChanged pra UI atualizar.
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }

        /// <summary>
        /// Tenta gastar uma quantia de ouro. Retorna true se conseguiu (tinha
        /// saldo suficiente), false caso contrário. Útil pra construir torres,
        /// comprar upgrades, etc.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (currentGold < amount) return false;
            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }

        /// <summary>
        /// Aplica dano à fortaleza (ex: quando um inimigo chega ao despawn).
        /// Se o HP zerar, dispara automaticamente o Game Over.
        /// </summary>
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
        /// Avança pra próxima wave. Notifica via OnWaveChanged.
        /// Se atingiu a totalWaves, isso é responsabilidade do WaveManager
        /// chamar TriggerVictory — este método só incrementa o contador.
        /// </summary>
        public void StartNewWave()
        {
            currentWave++;
            OnWaveChanged?.Invoke(currentWave, totalWaves);
        }

        /// <summary>
        /// Força o estado de Game Over (ex: HP da fortaleza zerou, ou outra
        /// condição de derrota). Dispara OnGameOver pra a UI mostrar a tela.
        /// </summary>
        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;
            OnGameOver?.Invoke();
            SceneManager.LoadScene("GameOver");
        }

        /// <summary>
        /// Marca vitória (ex: WaveManager terminou todas as waves sem perder).
        /// Dispara OnVictory pra a UI mostrar a tela de vitória.
        /// </summary>
        public void TriggerVictory()
        {
            if (isVictory || isGameOver) return;
            isVictory = true;
            OnVictory?.Invoke();
        }

        /// <summary>
        /// Volta o jogo ao estado inicial: ouro reseta pra startingGold,
        /// HP da fortaleza pra max, wave zerada, flags limpos.
        /// Útil pro botão "Tentar de novo" na tela de Game Over.
        /// </summary>
        public void ResetGame()
        {
            currentGold = startingGold;
            currentFortressHP = maxFortressHP;
            currentWave = 0;
            isGameOver = false;
            isVictory = false;

            // Notifica os ouvintes pra UI redesenhar com os valores iniciais.
            OnGoldChanged?.Invoke(currentGold);
            OnFortressHPChanged?.Invoke(currentFortressHP, maxFortressHP);
            OnWaveChanged?.Invoke(currentWave, totalWaves);
        }
    }
}
