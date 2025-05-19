using UnityEngine;
using Unity.Cinemachine;

public class CameraEffects : MonoBehaviour
{
    public CinemachineCamera cineCam;
    public float maxRollAngle = 10f;
    public float rollSpeed = 5f;

    private float currentRoll = 0f;
    public CinemachineImpulseSource impulseSource;

    void Update()
    {
        // Get player horizontal input (A/D, left/right)
        float horizontalInput = Input.GetAxis("Horizontal");

        // Target roll angle based on input
        float targetRoll = -horizontalInput * maxRollAngle;

        // Smoothly interpolate current roll
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSpeed);

        // Apply roll to the camera's Rotation Override
        var rotation = cineCam.transform.localRotation.eulerAngles;
        rotation.z = currentRoll;
        cineCam.transform.localRotation = Quaternion.Euler(rotation);

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Shake(0.2f);
        }
    }


    public void Shake(float intensity)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(intensity);
        }
    }
}
