using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private Transform mainCameraTransform;

    private void Start()
    {
        // Find the main camera in the scene.
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera not found. Make sure your camera has the 'MainCamera' tag.");
        }
    }

    private void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        // Get the direction to the camera but ignore the Y component.
        Vector3 directionToCamera = mainCameraTransform.position - transform.position;
        directionToCamera.y = 0; // Lock Y rotation.

        // If the direction vector is valid, update rotation.
        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }
}
