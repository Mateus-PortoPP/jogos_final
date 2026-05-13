using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Define os inimigos que spawnam numa única onda.
    ///
    /// Pode misturar tipos diferentes (ex: 3 goblins comuns + 1 goblin de armadura).
    /// Os inimigos são spawnados na ordem da lista: todos do tipo 1, depois todos
    /// do tipo 2, etc. Se quiser intercalar a ordem, divida em mais ondas.
    /// </summary>
    [System.Serializable]
    public class WaveData
    {
        [Tooltip("Nome da onda (ex: 'Onda 1') — só pra ajudar a identificar no Inspector e nos logs.")]
        public string waveName;

        [Tooltip("Tempo (em segundos) entre cada spawn dentro da onda.")]
        public float spawnInterval = 0.5f;

        [Tooltip("Lista de tipos de inimigos que spawnam nesta onda, com a quantidade de cada.")]
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
    }

    /// <summary>
    /// Um "lote" de inimigos do mesmo tipo dentro de uma onda.
    /// </summary>
    [System.Serializable]
    public class EnemySpawn
    {
        [Tooltip("Prefab do inimigo (Goblin, Goblin_Armadura, etc).")]
        public GameObject prefab;

        [Tooltip("Quantos deste prefab spawnam nesta onda.")]
        public int count = 1;
    }
}
