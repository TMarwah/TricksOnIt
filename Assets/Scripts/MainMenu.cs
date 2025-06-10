using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public RectTransform title; // Reference to the title canvas
    public float pulseSpeed = 1f; // Speed of the pulsing effect
    public float maxScale = 1.05f; // Maximum scale for pulsing
    public float minScale = 0.95f; // Minimum scale for pulsing
    public float maxRotation = 2f; // Maximum random rotation angle
    public float rotationLerpSpeed = 2f; // Speed of rotation interpolation

    private RectTransform titleRect;
    private float pulseTimer;
    private float targetRotation;
    private float currentRotation;

    private void Start()
    {
        // Play the main menu music
        MusicManager.Instance.PlayMusic("Lobby");

        // Get the RectTransform of the title canvas
        if (title != null)
        {
            titleRect = title.GetComponent<RectTransform>();
        }

        // Set an initial random target rotation
        targetRotation = Random.Range(-maxRotation, maxRotation);
    }

    private void Update()
    {
        if (titleRect != null)
        {
            // Pulse the scale
            pulseTimer += Time.deltaTime * pulseSpeed;
            float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(pulseTimer) + 1f) / 2f);
            titleRect.localScale = new Vector3(scale, scale, 1f);

            // Smoothly interpolate towards the target rotation
            currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
            titleRect.localRotation = Quaternion.Euler(0f, 0f, currentRotation);

            // If the pulse completes a cycle, choose a new target rotation
            if (Mathf.Sin(pulseTimer) < -0.99f) // Adjust threshold as needed
            {
                targetRotation = Random.Range(-maxRotation, maxRotation);
            }
        }
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
