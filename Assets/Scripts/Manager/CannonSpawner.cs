using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Instancia canhões nos spawn points conforme GameManager.CannonCount cresce.
    ///
    /// Comportamento:
    ///   - Start: spawna os canhões já comprados (se o jogador veio de uma cena anterior).
    ///   - OnCannonCountChanged: spawna o canhão novo imediatamente na próxima plataforma livre.
    ///   - Não despawna canhões (CannonCount só sobe).
    ///   - Ordem: spawnPoints[0] = primeiro canhão (esquerda → direita).
    ///
    /// Setup:
    ///   - 'spawnPoints' = Transforms pré-determinados em cada plataforma
    ///   - 'cannonPrefab' = prefab do canhão (com CannonTurret)
    /// </summary>
    public class CannonSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject cannonPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private int spawnedCount;

        private void Start()
        {
            SyncCannons();
            if (GameManager.Instance != null)
                GameManager.Instance.OnCannonCountChanged += HandleCountChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCannonCountChanged -= HandleCountChanged;
        }

        private void HandleCountChanged(int newCount) => SyncCannons();

        private void SyncCannons()
        {
            if (cannonPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

            int target = GameManager.Instance != null ? GameManager.Instance.CannonCount : 0;
            int maxSpawn = Mathf.Min(target, spawnPoints.Length);

            while (spawnedCount < maxSpawn)
            {
                var sp = spawnPoints[spawnedCount];
                spawnedCount++;
                if (sp == null) continue;
                Instantiate(cannonPrefab, sp.position, Quaternion.identity, sp);
            }

            if (target > spawnPoints.Length)
            {
                Debug.LogWarning($"[CannonSpawner] Player tem {target} canhões mas só há {spawnPoints.Length} slots na cena.");
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
