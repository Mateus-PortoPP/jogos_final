using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.Manager
{
    /// <summary>
    /// HUD da vida da fortaleza: usa uma Image com Image Type = Filled e
    /// muda fillAmount conforme o HP do castelo. Inscreve-se no evento
    /// OnFortressHPChanged do GameManager pra atualizar automaticamente.
    ///
    /// Setup esperado:
    /// - Image Type = Filled
    /// - Fill Method = Horizontal
    /// - Fill Origin = Left
    /// - Source Image = sprite Fortress_Life
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FortressLifeDisplay : MonoBehaviour
    {
        private Image bar;
        private GameManager game;

        private void Awake()
        {
            bar = GetComponent<Image>();
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogWarning("[FortressLifeDisplay] GameManager.Instance é null. Coloque um GameManager na cena.");
                return;
            }

            game.OnFortressHPChanged += UpdateBar;
            // Desenha estado inicial (Awake do GameManager já fez Reset, mas
            // a inscrição acontece depois — então puxamos manualmente aqui).
            UpdateBar(game.CurrentFortressHP, game.MaxFortressHP);
        }

        private void OnDestroy()
        {
            if (game != null) game.OnFortressHPChanged -= UpdateBar;
        }

        private void UpdateBar(int current, int max)
        {
            if (bar == null || max <= 0) return;
            bar.fillAmount = (float)current / max;
        }
    }
}
