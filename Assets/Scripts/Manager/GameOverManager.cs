using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense.Manager
{
    public class GameOverManager : MonoBehaviour
    {
        [Header("Fundo")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite victorySprite;
        [SerializeField] private Sprite defeatSprite;

        private void Start()
        {
            bool isVictory = GameManager.Instance != null && GameManager.Instance.IsVictory;

            if (backgroundImage != null)
                backgroundImage.sprite = isVictory ? victorySprite : defeatSprite;
        }

        public void RestartGame()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            SceneManager.LoadScene("Menu_Inicial");
        }
    }
}
