using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Common;

namespace TowerDefense.Player
{
    /// <summary>
    /// HUD do coração: troca o sprite conforme o HP do jogador cai.
    /// heartStages[0] = vida cheia. Último índice = morto.
    /// Tamanho esperado do array = maxHealth + 1 (estado cheio + 1 por dano).
    ///
    /// Funciona tanto com SpriteRenderer (sprite no mundo) quanto com Image (UI/Canvas).
    /// Pra HUD que segue a câmera, prefira usar Image dentro de um Canvas.
    /// </summary>
    public class PlayerLifeDisplay : MonoBehaviour
    {
        [Tooltip("Health do jogador a observar (arraste o GameObject Player aqui).")]
        [SerializeField] private Health playerHealth;

        [Tooltip("Sprites por nível de dano: [0]=cheio, [N-1]=morto. Total = maxHealth + 1.")]
        [SerializeField] private Sprite[] heartStages;

        private SpriteRenderer sr;
        private Image image;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            image = GetComponent<Image>();
            var anim = GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
        }

        private void OnEnable()
        {
            if (playerHealth == null) return;
            playerHealth.Damaged += OnDamaged;
        }

        private void Start()
        {
            // Em Start, todos os Awakes (incluindo Health.Awake) já rodaram,
            // então Current já reflete maxHealth. OnEnable é cedo demais.
            if (playerHealth != null)
                UpdateSprite(playerHealth.Current, playerHealth.Max);
        }

        private void OnDisable()
        {
            if (playerHealth != null) playerHealth.Damaged -= OnDamaged;
        }

        private void OnDamaged(int current, int max) => UpdateSprite(current, max);

        private void UpdateSprite(int current, int max)
        {
            if (heartStages == null || heartStages.Length == 0 || max <= 0) return;
            int n = heartStages.Length;

            int idx;
            if (current <= 0)
            {
                idx = n - 1;          // morto: último sprite (vazio)
            }
            else if (current >= max)
            {
                idx = 0;              // vida cheia: primeiro sprite
            }
            else
            {
                // Fração de dano (0 = cheio, ~1 = quase morto) mapeada nos
                // sprites intermediários [1 .. n-2]. Garante que o último
                // sprite (vazio) só apareça quando current == 0.
                float damageFraction = 1f - (float)current / max;
                int intermediate = Mathf.Clamp(Mathf.FloorToInt(damageFraction * (n - 2)), 0, n - 3);
                idx = 1 + intermediate;
            }

            Sprite chosen = heartStages[idx];
            if (sr != null) sr.sprite = chosen;
            if (image != null) image.sprite = chosen;
        }
    }
}
