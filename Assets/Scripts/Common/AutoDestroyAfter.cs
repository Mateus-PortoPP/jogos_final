using UnityEngine;

namespace TowerDefense.Common
{
    /// <summary>
    /// Destroi o GameObject após X segundos. Útil pra VFX one-shot.
    /// </summary>
    public class AutoDestroyAfter : MonoBehaviour
    {
        [SerializeField] private float seconds = 0.5f;

        private void Start()
        {
            Destroy(gameObject, seconds);
        }
    }
}
