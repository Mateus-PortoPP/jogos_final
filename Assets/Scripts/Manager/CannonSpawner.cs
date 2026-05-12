using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Instancia canhões no início da cena de gameplay baseado em GameManager.CannonCount.
    ///
    /// Setup na cena:
    ///   - GameObject vazio com este script
    ///   - 'spawnPoints' = array de Transforms (pontos pré-determinados no mapa)
    ///   - 'cannonPrefab' = prefab do canhão (com CannonTurret)
    ///
    /// Lógica: instancia min(CannonCount, spawnPoints.Length) canhões nos primeiros
    /// spawn points. Cada upgrade compra adiciona +1 canhão até esgotar slots.
    /// </summary>
    public class CannonSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject cannonPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private void Start()
        {
            if (cannonPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

            int count = GameManager.Instance != null ? GameManager.Instance.CannonCount : 0;
            int spawn = Mathf.Min(count, spawnPoints.Length);

            for (int i = 0; i < spawn; i++)
            {
                if (spawnPoints[i] == null) continue;
                Instantiate(cannonPrefab, spawnPoints[i].position, Quaternion.identity, spawnPoints[i]);
            }

            if (count > spawnPoints.Length)
            {
                Debug.LogWarning($"[CannonSpawner] Player tem {count} canhões mas só há {spawnPoints.Length} slots na cena.");
            }
        }

        private void OnDrawGizmos()
        {
            if (spawnPoints == null) return;
            Gizmos.color = Color.cyan;
            foreach (var p in spawnPoints)
            {
                if (p == null) continue;
                Gizmos.DrawWireCube(p.position, Vector3.one * 0.5f);
            }
        }
    }
}
