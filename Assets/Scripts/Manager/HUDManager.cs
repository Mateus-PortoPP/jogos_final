using TMPro;
using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Gerencia os elementos de HUD em jogo (ouro, HP da fortaleza, wave).
    /// Inscreve-se nos eventos do GameManager e redesenha os textos/sliders.
    ///
    /// O HUDManager NÃO conhece quem mudou o valor (Goblin que morreu, torre
    /// que foi vendida, fortaleza que tomou dano) — só recebe a notificação
    /// pelo evento e atualiza a UI. Isso desacopla a UI da lógica de jogo.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Ouro")]
        [Tooltip("Texto que mostra o saldo de ouro do jogador.")]
        [SerializeField] private TextMeshProUGUI goldText;

        private GameManager game;

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogWarning("[HUDManager] GameManager.Instance é null. Coloque um GameManager na cena.");
                return;
            }

            // Inscreve nos eventos.
            game.OnGoldChanged += UpdateGoldText;

            // Desenha valores iniciais (o GameManager já fez Reset no Awake,
            // mas a inscrição acima é depois desse evento, então puxamos manualmente).
            UpdateGoldText(game.CurrentGold);
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.OnGoldChanged -= UpdateGoldText;
            }
        }

        private void UpdateGoldText(int gold)
        {
            if (goldText != null)
                goldText.text = gold.ToString();
        }
    }
}
