using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    public class GameOverManager : MonoBehaviour
    {
        public void RestartGame()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            SceneManager.LoadScene("Menu_Inicial");
        }
    }
}
