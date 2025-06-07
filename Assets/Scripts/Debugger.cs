using UnityEngine;

public class Debugger : MonoBehaviour
{
    private GameManager gameManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the GameManager component from the same GameObject
        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager component not found on this GameObject.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleDebugInput();
    }

    private void HandleDebugInput()
    {
        // Debug keys for level changes
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeLevel(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeLevel(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeLevel(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeLevel(3);
    }

    private void ChangeLevel(int levelIndex)
    {
        if (gameManager != null)
        {
            gameManager.ChangeLevel(levelIndex);
        }
        else
        {
            Debug.LogError("GameManager is not assigned.");
        }
    }

    private void TogglePause()
    {
        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
        else
        {
            Debug.LogError("GameManager is not assigned.");
        }
    }
}
