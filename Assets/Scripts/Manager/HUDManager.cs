using TMPro;
using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// HUD em jogo: ouro, contador de noite, contador de onda.
    /// Inscreve-se nos eventos do GameManager + WaveManager e redesenha os textos.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Ouro")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private string goldFormat = "🪙 {0}";

        [Header("Noite / Onda")]
        [SerializeField] private TextMeshProUGUI nightText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private string nightFormat = "Noite {0}/{1}";
        [SerializeField] private string waveFormat = "Onda {0}/{1}";

        private GameManager game;
        private WaveManager wave;

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogWarning("[HUDManager] GameManager.Instance é null. Coloque um GameManager no Menu_Inicial.");
                return;
            }

            game.OnGoldChanged += UpdateGoldText;
            game.OnNightChanged += UpdateNightText;
            game.OnWaveChanged += UpdateWaveText;

            UpdateGoldText(game.CurrentGold);
            UpdateNightText(game.CurrentNight, game.TotalNights);
            UpdateWaveText(game.CurrentWave, 0);

            // WaveManager pode estar pronto agora ou em 1 frame.
            wave = WaveManager.Instance;
            if (wave != null) Subscribe(wave);
            else Invoke(nameof(LateSubscribe), 0.05f);
        }

        private void LateSubscribe()
        {
            wave = WaveManager.Instance;
            if (wave != null) Subscribe(wave);
            // Já atualiza o texto com o total descoberto agora.
            UpdateWaveText(game.CurrentWave, wave != null ? wave.TotalWaves : 0);
        }

        private void Subscribe(WaveManager wm)
        {
            // Quando uma onda inicia, atualizamos com o total real do WaveManager
            wm.OnWaveStarted += idx => UpdateWaveText(idx + 1, wm.TotalWaves);
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.OnGoldChanged -= UpdateGoldText;
                game.OnNightChanged -= UpdateNightText;
                game.OnWaveChanged -= UpdateWaveText;
            }
        }

        private void UpdateGoldText(int gold)
        {
            if (goldText != null) goldText.text = string.Format(goldFormat, gold);
        }

        private void UpdateNightText(int night, int total)
        {
            if (nightText != null) nightText.text = string.Format(nightFormat, night, total);
        }

        private void UpdateWaveText(int current, int total)
        {
            if (waveText != null)
            {
                int totalShown = total > 0 ? total : (wave != null ? wave.TotalWaves : 0);
                waveText.text = string.Format(waveFormat, Mathf.Max(0, current), totalShown);
            }
        }
    }
}
