using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private ThirdPersonMovement player;
    public Transform camTransform;
    [Header("Camera FOV")]
    public Unity.Cinemachine.CinemachineCamera virtualCamera;
    public float normalFOV = 60f;
    public float sprintFOV = 70f;
    public float aimFOV = 40f;
    public float fovLerpSpeed = 5f;
    private float currentFOV;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        player = GetComponent<ThirdPersonMovement>();
        virtualCamera = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (virtualCamera != null)
        {
            camTransform = virtualCamera.transform;
            currentFOV = virtualCamera.Lens.FieldOfView;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCameraFOV();
    }

    void UpdateCameraFOV()
    {
        if (virtualCamera == null) return;

        float targetFOV = normalFOV;

        if (player.isAiming)
        {
            targetFOV = aimFOV;
            FaceCamera();
        }
        else if (player.isSprinting)
        {
            targetFOV = sprintFOV;
        }

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovLerpSpeed);
        var lens = virtualCamera.Lens;
        lens.FieldOfView = currentFOV;
        virtualCamera.Lens = lens;
    }

    void FaceCamera()
    {
        Vector3 cameraForward = camTransform.forward;
        cameraForward.y = 0f;
        if (cameraForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = targetRotation;
        }
    }
}
