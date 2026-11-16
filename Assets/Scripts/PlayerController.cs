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

    [Header("Inventory & Hand Stats")]
    public Transform handSocket;
    public float pickUpRange = 4.0f;
    [HideInInspector] public int inventorySize = 3;
    public Transform itemDropPoint;

    [Header("UI & Phone References")]
    [SerializeField] private GameObject worldDimPanel;
    [SerializeField] private GameObject phoneView;
    [SerializeField] private PhoneUIController phoneUiController;
    [SerializeField] private InputActionReference pauseActionReference;
    [SerializeField] private InputActionReference openBrowserActionReference;
    public TextMeshProUGUI interactionPromptText;

    [Header("Input References")]
    public InputActionReference moveAction;
    public InputActionReference sprintAction;
    public InputActionReference jumpAction;
    public InputActionReference lookAction;
    public InputActionReference dropAction;
    public InputActionReference pickAction;
    public InputActionReference interactAction;
    public InputActionReference freeHandsAction;
    [Header("Input References - UI")]
    public InputActionReference closeAction;

    [HideInInspector] public Camera mainCamera;
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public bool isDrawingMode = false;

    private PlayerMovementHandler movementHandler;
    private PlayerLookHandler lookHandler;
    private PlayerInteractionHandler interactionHandler;
    private PlayerInventoryHandler inventoryHandler;

    public PlayerInventoryHandler InventoryHandler => inventoryHandler;

    private void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();

        if (phoneUiController == null)
            phoneUiController = GetComponent<PhoneUIController>();

        // Editor can keep timeScale=0 after exiting Play while the phone pause was open.
        isPaused = false;
        Time.timeScale = 1f;

        LockCursor();

        movementHandler = new PlayerMovementHandler(this);
        lookHandler = new PlayerLookHandler(this);
        inventoryHandler = new PlayerInventoryHandler(this);
        interactionHandler = new PlayerInteractionHandler(this);

        if (worldDimPanel != null) worldDimPanel.SetActive(false);
        if (phoneView != null) phoneView.SetActive(false);
        phoneUiController?.Hide();
        if (interactionPromptText != null) interactionPromptText.text = "";
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        sprintAction?.action.Enable();
        jumpAction?.action.Enable();
        lookAction?.action.Enable();
        dropAction?.action.Enable();
        pickAction?.action.Enable();
        interactAction?.action.Enable();
        freeHandsAction?.action.Enable();
        closeAction?.action.Enable();
        pauseActionReference?.action.Enable();
        openBrowserActionReference?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        sprintAction?.action.Disable();
        jumpAction?.action.Disable();
        lookAction?.action.Disable();
        dropAction?.action.Disable();
        pickAction?.action.Disable();
        interactAction?.action.Disable();
        freeHandsAction?.action.Disable();
        closeAction?.action.Disable();
        pauseActionReference?.action.Disable();
        openBrowserActionReference?.action.Disable();
    }

    private void Update()
    {
        HandlePhoneInput();

        if (isPaused || isDrawingMode) return;

        movementHandler.Update();
        interactionHandler.Update();
        inventoryHandler.Update();
    }

    private void LateUpdate()
    {
        if (isPaused || isDrawingMode) return;

        lookHandler.LateUpdate();
        movementHandler.LateUpdate();
    }

    private void HandlePhoneInput()
    {
        if (isDrawingMode)
            return;

        if (pauseActionReference != null && pauseActionReference.action.WasPressedThisFrame())
        {
            if (isPaused && phoneUiController != null && phoneUiController.ActiveScreen == PhoneScreenId.Pause)
                ClosePhoneModal();
            else
                OpenPhoneModal(PhoneScreenId.Pause);
            return;
        }

        if (openBrowserActionReference != null && openBrowserActionReference.action.WasPressedThisFrame())
        {
            if (isPaused && phoneUiController != null && phoneUiController.ActiveScreen == PhoneScreenId.BrowserFeed)
                ClosePhoneModal();
            else
                OpenPhoneModal(PhoneScreenId.BrowserFeed);
        }
    }

    public void OpenPhoneModal(PhoneScreenId screenId)
    {
        isPaused = true;
        Time.timeScale = 0f;
        UnlockCursor();

        if (interactionPromptText != null)
            interactionPromptText.text = "";

        SetCrosshairVisible(false);

        if (phoneUiController != null)
            phoneUiController.Show(screenId);
        else
        {
            if (phoneView != null) phoneView.SetActive(true);
            if (worldDimPanel != null) worldDimPanel.SetActive(true);
        }
    }

    public void ClosePhoneModal()
    {
        isPaused = false;
        Time.timeScale = 1f;
        LockCursor();

        if (phoneUiController != null)
            phoneUiController.Hide();
        else
        {
            if (phoneView != null) phoneView.SetActive(false);
            if (worldDimPanel != null) worldDimPanel.SetActive(false);
        }

        SetCrosshairVisible(true);
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (phoneUiController != null)
        {
            phoneUiController.SetCrosshairVisible(visible);
            return;
        }

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Crosshair")
                t.gameObject.SetActive(visible);
        }
    }

    /// <summary>Legacy entry used by older pause UI hooks.</summary>
    public void TogglePause()
    {
        if (isPaused)
            ClosePhoneModal();
        else
            OpenPhoneModal(PhoneScreenId.Pause);
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
