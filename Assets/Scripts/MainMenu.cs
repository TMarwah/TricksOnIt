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
        MusicManager.Instance.PlayMusic("Level1");
        SceneManager.LoadScene("SampleScene");
    }
    public void Level2()
    {
        SceneManager.LoadScene("Enemy Pathfinding");
    }
    public void Level3()
    {
        SceneManager.LoadScene("Enemy Pathfinding");
    }
    public void Quit()
    {
        Application.Quit();
    }
}
