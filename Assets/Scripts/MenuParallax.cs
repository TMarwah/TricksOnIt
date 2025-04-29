using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    public float offsetMultiplier = 1f;
    public float smoothTime = .3f;

    private Vector2 startPos;
    private Vector3 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2.offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        transform.position = Vector3.SmoothDamp(transform.position, startPos + (offset * offsetMultiplier), ref velocity, smoothTime);
    }
}
