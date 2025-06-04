using UnityEngine;

public class SlideCamera : MonoBehaviour
{
    public float speed = 10f; // Speed of the camera movement
    public float minZ = -200f; // Starting Z position
    public float maxZ = 300f; // Ending Z position
    private bool isFading = false; // To track if fading is in progress
    private CanvasGroup fadeCanvasGroup; // CanvasGroup for fade effect

    void Start()
    {
        // Initialize the camera position
        transform.position = new Vector3(transform.position.x, transform.position.y, minZ);

        // Create a fade effect CanvasGroup
        GameObject fadeCanvas = new GameObject("FadeCanvas");
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvasGroup = fadeCanvas.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f; // Start with no fade
    }

    void Update()
    {
        if (!isFading)
        {
            // Move the camera along the Z-axis
            transform.position += Vector3.forward * speed * Time.deltaTime;

            // Check if the camera has reached the max Z position
            if (transform.position.z >= maxZ)
            {
                StartCoroutine(FadeAndReset());
            }
        }
    }

    private System.Collections.IEnumerator FadeAndReset()
    {
        isFading = true;

        // Fade to black
        for (float t = 0; t <= 1; t += Time.deltaTime)
        {
            fadeCanvasGroup.alpha = t;
            yield return null;
        }

        // Reset the camera position
        transform.position = new Vector3(transform.position.x, transform.position.y, minZ);

        // Fade back to clear
        for (float t = 1; t >= 0; t -= Time.deltaTime)
        {
            fadeCanvasGroup.alpha = t;
            yield return null;
        }

        isFading = false;
    }
}
