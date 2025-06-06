using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Start()
    {
        // Play the main menu music
        MusicManager.Instance.PlayMusic("Lobby");
    }
    public void Level1()
    {
        MusicManager.Instance.StopMusic();
        SceneManager.LoadScene("MainGame");
    }
    public void Quit()
    {
        Application.Quit();
    }
}
