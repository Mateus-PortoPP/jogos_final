using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    public class MenuManager : MonoBehaviour
    {
        public void StartGame()
        {
            SceneManager.LoadScene("inicial");
        }
    }
}
