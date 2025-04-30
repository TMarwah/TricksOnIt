using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Menu"); // Replace "MainMenu" with your actual scene name
            // or use: SceneManager.LoadScene(0); for build index 0
        }
    }
}