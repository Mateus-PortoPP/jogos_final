using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// Barra de vida em formato de coração: um coração escuro de fundo e um
    /// coração vermelho na frente com Image.type=Filled. O preenchimento
    /// vermelho diminui suavemente conforme o HP cai (fração Current/Max),
    /// dando granularidade contínua em vez de sprites discretos.
    /// </summary>
    public class PlayerHeartFill : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [Tooltip("Image (type=Filled) que representa a parte vermelha/preenchida do coração.")]
        [SerializeField] private Image fillImage;
        [Tooltip("Velocidade da animação do preenchimento descendo (unidades de fração por segundo).")]
        [SerializeField] private float lerpSpeed = 2.5f;

        private float targetFill = 1f;

        private void OnEnable()
        {
            if (playerHealth != null) playerHealth.Damaged += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (playerHealth != null) playerHealth.Damaged -= OnHealthChanged;
        }

        private void Start()
        {
            if (playerHealth != null) SetTarget(playerHealth.Current, playerHealth.Max);
            if (fillImage != null) fillImage.fillAmount = targetFill;
        }

        private void OnHealthChanged(int current, int max) => SetTarget(current, max);

        private void SetTarget(int current, int max)
        {
            targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        }

        private void Update()
        {
            if (fillImage == null) return;
            fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, lerpSpeed * Time.unscaledDeltaTime);
        }
    }
}
