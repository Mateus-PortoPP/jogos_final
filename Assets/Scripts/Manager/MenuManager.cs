using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private string firstSceneName = "Cutscene_Inicial";

        public void StartGame()
        {
            SceneManager.LoadScene(firstSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
