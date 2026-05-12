using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Configuração de ondas para UMA noite específica.
    ///
    /// Como criar um asset:
    ///   Project window → click direito → Create → TowerDefense → Wave Set
    ///   Renomeie pra algo como "WaveSet_Noite1.asset", configure a lista no Inspector.
    ///
    /// Depois arraste esse asset no campo correspondente do WaveManager
    /// (que tem um array com 1 WaveSet por noite).
    /// </summary>
    [CreateAssetMenu(fileName = "WaveSet_NoiteX", menuName = "TowerDefense/Wave Set", order = 0)]
    public class WaveSet : ScriptableObject
    {
        [Tooltip("Nome amigável da noite — só aparece no Inspector.")]
        public string nightLabel = "Noite";

        [Tooltip("Lista de ondas que compõem esta noite. Tamanho = total de ondas da noite.")]
        public List<WaveData> waves = new List<WaveData>();
    }
}
