using UnityEngine;

public class PlayerLookHandler
{
    private PlayerController owner;
    private float verticalRotation = 0f;

    public PlayerLookHandler(PlayerController owner)
    {
        this.owner = owner;
    }

    public void LateUpdate()
    {
        if (owner.lookAction == null || owner.mainCamera == null) return;

        Vector2 lookInput = owner.lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * owner.lookSensitivity;
        float mouseY = lookInput.y * owner.lookSensitivity;

        owner.transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -owner.maxLookAngle, owner.maxLookAngle);
        owner.mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}