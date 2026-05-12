using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Após todas as ondas da noite terminarem, espera o jogador pressionar Enter
    /// pra carregar a próxima cena (UpgradeSelect, Cutscene ou Victory).
    ///
    /// Exceção: para a(s) noite(s) em que existe um DoorTrigger cuidando da
    /// transição (default: noite 2), este controller fica QUIETO — deixe o
    /// item correspondente do array 'nextSceneByNight' vazio.
    ///
    /// Coloque um GameObject vazio com este componente em cada cena de gameplay.
    /// Configure 'nextSceneByNight' (index 0 = noite 1) conforme o fluxo:
    ///
    /// Gameplay_Interno:
    ///   [0] = "UpgradeSelect"      // pós N1
    ///   [1] = ""                   // pós N2 → DoorTrigger cuida
    ///
    /// Gameplay_Externo:
    ///   [2] = "UpgradeSelect"      // pós N3
    ///   [3] = "Victory"            // pós N4
    /// </summary>
    public class NightEndController : MonoBehaviour
    {
        [Tooltip("Index 0 = noite 1, 1 = noite 2, etc. String vazia = não auto-transiciona (DoorTrigger cuida).")]
        [SerializeField] private string[] nextSceneByNight = { "UpgradeSelect", "", "UpgradeSelect", "Victory" };

        private bool readyToProceed;
        private string targetScene;

        private void OnEnable()
        {
            if (WaveManager.Instance != null) Subscribe();
            else Invoke(nameof(Subscribe), 0.05f);
        }

        private void Subscribe()
        {
            if (WaveManager.Instance == null) return;
            WaveManager.Instance.OnAllWavesCompleted += HandleAllWavesCompleted;
        }

        private void OnDisable()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted -= HandleAllWavesCompleted;
        }

        private void HandleAllWavesCompleted()
        {
            int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1;
            int idx = night - 1;
            if (idx < 0 || idx >= nextSceneByNight.Length) return;
            string scene = nextSceneByNight[idx];
            if (string.IsNullOrEmpty(scene)) return; // door handles it

            targetScene = scene;
            readyToProceed = true;
            Debug.Log($"[NightEndController] Noite {night} encerrada. Aperte Enter pra ir para '{targetScene}'.");
        }

        private void Update()
        {
            if (!readyToProceed) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                readyToProceed = false;
                SceneFader.GetOrCreate().FadeOutAndLoad(targetScene);
            }
        }
    }
}
