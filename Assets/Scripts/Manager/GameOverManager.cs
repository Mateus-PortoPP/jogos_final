using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    public class GameOverManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Vitória")]
        [SerializeField] private string victoryTitle =
            "VITÓRIA\nReino Defendido\n\n<size=60%><i>O reino está a salvo.\nO Guardião descansa.</i></size>";

        [Header("Derrota")]
        [SerializeField] private string defeatTitle = "DERROTA";

        private void Start()
        {
            if (titleText == null)
                titleText = FindObjectOfType<TextMeshProUGUI>();

            if (titleText == null) return;

            bool isVictory = GameManager.Instance != null && GameManager.Instance.IsVictory;
            titleText.text = isVictory ? victoryTitle : defeatTitle;
        }

        public void RestartGame()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            SceneManager.LoadScene("Menu_Inicial");
        }
    }
}
