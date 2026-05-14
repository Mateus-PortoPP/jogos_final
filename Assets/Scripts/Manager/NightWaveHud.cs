using TMPro;
using UnityEngine;

namespace TowerDefense.Manager
{
    public class NightWaveHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private string format = "Noite {0} — Onda {1}/{2}";

        private void OnEnable()
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 0;
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveNumber : 0;
            int total = WaveManager.Instance != null ? WaveManager.Instance.TotalWaves : 0;
            if (wave < 1) wave = 1;
            label.text = string.Format(format, night, wave, total);
        }
    }
}
