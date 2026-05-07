using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Dados de configuração de uma única onda. Não é MonoBehaviour nem
    /// ScriptableObject — é só uma estrutura serializável que aparece como item
    /// expansível na lista de waves do WaveManager no Inspector.
    /// </summary>
    [System.Serializable]
    public class WaveData
    {
        [Tooltip("Nome da onda (ex: 'Onda 1') — só pra ajudar a identificar no Inspector e nos logs.")]
        public string waveName;

        [Tooltip("Quantos inimigos spawnam nesta onda.")]
        public int enemyCount;

        [Tooltip("Tempo (em segundos) entre cada spawn dentro da onda.")]
        public float spawnInterval;

        [Tooltip("Prefab do inimigo a spawnar (geralmente o Goblin).")]
        public GameObject enemyPrefab;
    }
}
