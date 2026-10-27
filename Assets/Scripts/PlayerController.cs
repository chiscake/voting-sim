using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float walkSpeed = 5.5f;
    public float runningSpeed = 9.0f;
    public float jumpForce = 8.0f;
    public float gravity = 20.0f;

    [Header("Headbobbing Stats")]
    public bool enableHeadBob = true;
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float sprintBobSpeed = 18f;
    public float sprintBobAmount = 0.09f;

    [Header("Camera & Look Stats")]
    public float lookSensitivity = 0.2f;
    public float maxLookAngle = 80f;

    [Header("Interaction Stats")]
    public Transform holdPosition;
    public float pickUpRange = 3.0f;
    public float throwForce = 12.0f;

    [Header("Inventory Stats")]
    [HideInInspector] public int inventorySize = 3;
    public Transform itemDropPoint;

    [Header("UI & Pause References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private InputActionReference pauseActionReference;
    public TextMeshProUGUI interactionPromptText;

    [Header("Input References")]
    public InputActionReference moveAction;
    public InputActionReference sprintAction;
    public InputActionReference jumpAction;
    public InputActionReference lookAction;
    public InputActionReference grabAction;
    public InputActionReference throwAction;
    public InputActionReference dropAction;
    public InputActionReference pickAction;
    public InputActionReference interactAction; // Новое действие 'interact' (клавиша E)

    [HideInInspector] public Camera mainCamera;
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public bool isDrawingMode = false; // Флаг режима заполнения бюллетеня

    private PlayerMovementHandler movementHandler;
    private PlayerLookHandler lookHandler;
    private PlayerInteractionHandler interactionHandler;
    private PlayerInventoryHandler inventoryHandler;

    private void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();

        LockCursor();

        movementHandler = new PlayerMovementHandler(this);
        lookHandler = new PlayerLookHandler(this);
        interactionHandler = new PlayerInteractionHandler(this);
        inventoryHandler = new PlayerInventoryHandler(this);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (interactionPromptText != null) interactionPromptText.text = "";
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        sprintAction?.action.Enable();
        jumpAction?.action.Enable();
        lookAction?.action.Enable();
        grabAction?.action.Enable();
        throwAction?.action.Enable();
        dropAction?.action.Enable();
        pickAction?.action.Enable();
        interactAction?.action.Enable();
        pauseActionReference?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        sprintAction?.action.Disable();
        jumpAction?.action.Disable();
        lookAction?.action.Disable();
        grabAction?.action.Disable();
        throwAction?.action.Disable();
        dropAction?.action.Disable();
        pickAction?.action.Disable();
        interactAction?.action.Disable();
        pauseActionReference?.action.Disable();
    }

    private void Update()
    {
        HandlePauseInput();

        if (isPaused || isDrawingMode) return; // Если рисуем или на паузе — управление игроком заблокировано

        movementHandler.Update();
        interactionHandler.Update();
        inventoryHandler.Update();
    }

    private void LateUpdate()
    {
        if (isPaused || isDrawingMode) return;

        lookHandler.LateUpdate();
        movementHandler.LateUpdate();
        interactionHandler.LateUpdate();
    }

    private void HandlePauseInput()
    {
        if (pauseActionReference != null && pauseActionReference.action.WasPressedThisFrame())
        {
            if (!isDrawingMode) TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            UnlockCursor();
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            if (interactionPromptText != null) interactionPromptText.text = "";
        }
        else
        {
            Time.timeScale = 1f;
            LockCursor();
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}