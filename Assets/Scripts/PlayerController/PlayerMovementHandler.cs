using UnityEngine;

public class PlayerMovementHandler
{
    private PlayerController owner;
    private Vector3 moveDirection = Vector3.zero;
    private float bobTimer = 0f;
    private float defaultCameraPosY = 0f;

    public PlayerMovementHandler(PlayerController owner)
    {
        this.owner = owner;
        if (owner.mainCamera != null)
        {
            defaultCameraPosY = owner.mainCamera.transform.localPosition.y;
        }
    }

    public void Update()
    {
        if (owner.moveAction == null) return;

        Vector2 moveVector = owner.moveAction.action.ReadValue<Vector2>();
        bool isRunning = owner.sprintAction != null && owner.sprintAction.action.IsPressed();
        float currentSpeed = isRunning ? owner.runningSpeed : owner.walkSpeed;

        Vector3 forward = owner.transform.TransformDirection(Vector3.forward);
        Vector3 right = owner.transform.TransformDirection(Vector3.right);

        float verticalVelocity = moveDirection.y;
        Vector3 movement = (forward * (moveVector.y * currentSpeed)) + (right * (moveVector.x * currentSpeed));

        if (owner.characterController.isGrounded)
        {
            verticalVelocity = -0.5f;
            if (owner.jumpAction != null && owner.jumpAction.action.IsPressed())
            {
                verticalVelocity = owner.jumpForce;
            }
        }
        else
        {
            verticalVelocity -= owner.gravity * Time.deltaTime;
        }

        moveDirection = movement;
        moveDirection.y = verticalVelocity;

        owner.characterController.Move(moveDirection * Time.deltaTime);
    }

    public void LateUpdate()
    {
        if (!owner.enableHeadBob || owner.mainCamera == null) return;

        if (!owner.characterController.isGrounded)
        {
            ResetHeadBob();
            return;
        }

        Vector2 moveVector = owner.moveAction != null ? owner.moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        if (moveVector.sqrMagnitude > 0.1f)
        {
            bool isRunning = owner.sprintAction != null && owner.sprintAction.action.IsPressed();
            float currentSpeed = isRunning ? owner.sprintBobSpeed : owner.walkBobSpeed;
            float currentAmount = isRunning ? owner.sprintBobAmount : owner.walkBobAmount;

            bobTimer += Time.deltaTime * currentSpeed;

            float targetY = defaultCameraPosY + Mathf.Sin(bobTimer) * currentAmount;
            float targetX = Mathf.Cos(bobTimer * 0.5f) * (currentAmount * 0.5f);

            Vector3 targetPosition = new Vector3(targetX, targetY, owner.mainCamera.transform.localPosition.z);
            owner.mainCamera.transform.localPosition = Vector3.Lerp(owner.mainCamera.transform.localPosition, targetPosition, Time.deltaTime * 10f);
        }
        else
        {
            ResetHeadBob();
        }
    }

    private void ResetHeadBob()
    {
        if (owner.mainCamera == null) return;
        bobTimer = 0f;
        Vector3 targetPosition = new Vector3(0f, defaultCameraPosY, owner.mainCamera.transform.localPosition.z);
        owner.mainCamera.transform.localPosition = Vector3.Lerp(owner.mainCamera.transform.localPosition, targetPosition, Time.deltaTime * 6f);
    }
}