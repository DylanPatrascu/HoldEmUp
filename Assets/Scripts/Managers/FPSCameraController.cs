using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraController : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float horizontalViewAngle = 160f;
    [SerializeField] private float verticalViewAngle = 160f;

    [SerializeField] private Transform playerBody;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Quaternion initialPlayerBodyRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!playerBody) playerBody = transform.parent;

        if (playerBody) initialPlayerBodyRotation = playerBody.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerBody) return;

        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        float hvvAngle = verticalViewAngle * 0.5f;
        xRotation = Mathf.Clamp(xRotation, -hvvAngle, hvvAngle);

        yRotation += mouseX;
        float hhvAngle = horizontalViewAngle * 0.5f;
        yRotation = Mathf.Clamp(yRotation, -hhvAngle, hhvAngle);

        playerBody.localRotation = initialPlayerBodyRotation * Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
