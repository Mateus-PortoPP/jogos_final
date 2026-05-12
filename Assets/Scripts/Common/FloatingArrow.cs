using UnityEngine;

namespace TowerDefense.Common
{
    /// <summary>
    /// Anima um sprite "flutuante" (sobe e desce devagar) para indicar
    /// um ponto de interesse (porta, item, etc.). Acoplado a um Transform
    /// com um Sprite Renderer normal.
    /// </summary>
    public class FloatingArrow : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.25f;
        [SerializeField] private float frequency = 1.5f;

        private Vector3 startLocal;

        private void OnEnable()
        {
            startLocal = transform.localPosition;
        }

        private void Update()
        {
            float y = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
            transform.localPosition = startLocal + new Vector3(0f, y, 0f);
        }
    }
}
