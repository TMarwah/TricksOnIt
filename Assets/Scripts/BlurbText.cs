using UnityEngine;
using System.Collections;

public class BlurbText : MonoBehaviour
{
    public static BlurbText Instance { get; private set; }
    private TMPro.TextMeshProUGUI textMeshPro;
    [SerializeField] private float typingSpeed = 0.1f;
    [SerializeField] private float lingerDuration = 2f;

    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: Keep this instance across scenes

        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TMPro.TextMeshProUGUI>();
        }
    }

    void Start()
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = ""; // Ensure no text by default
        }
    }

    public void TypeText(string fullText)
    {
        if (textMeshPro != null)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine); // Stop any ongoing typing coroutine
            }

            textMeshPro.text = ""; // Reset text before typing
            typingCoroutine = StartCoroutine(TypeTextCoroutine(fullText));
        }
    }

    private IEnumerator TypeTextCoroutine(string fullText)
    {
        foreach (char c in fullText)
        {
            textMeshPro.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(lingerDuration);

        textMeshPro.text = "";
        typingCoroutine = null; // Clear the reference when done
    }
}
