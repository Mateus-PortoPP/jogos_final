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
    }
}
