using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Level1()
    {
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
