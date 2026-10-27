using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class BallotCanvasDrawer : MonoBehaviour
{
    [Header("Drawing Settings")]
    [SerializeField] private int brushSize = 5;
    [SerializeField] private Color penColor = Color.blue;

    [Header("Input References")]
    [SerializeField] private InputActionReference drawActionReference;
    [SerializeField] private InputActionReference closeActionReference;

    [Header("UI References")]
    [SerializeField] private RawImage ballotRawImage;

    private Texture2D activeBallotTexture;
    private RectTransform rectTransform;
    private PlayerController playerController;

    private Vector2 lastDrawPixelPos = Vector2.zero;
    private bool wasDrawingLastFrame = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        playerController = FindObjectOfType<PlayerController>();
    }

    private void OnEnable()
    {
        drawActionReference?.action.Enable();
        closeActionReference?.action.Enable();
    }

    private void OnDisable()
    {
        drawActionReference?.action.Disable();
        closeActionReference?.action.Disable();
    }

    // Метод для инициализации текстуры, вызываемый при открытии бюллетеня
    public void InitWithTexture(Texture2D sharedTexture)
    {
        activeBallotTexture = sharedTexture;
        if (ballotRawImage != null && activeBallotTexture != null)
        {
            ballotRawImage.texture = activeBallotTexture;
        }
    }

    private void Update()
    {
        if (closeActionReference != null && closeActionReference.action.WasPressedThisFrame())
        {
            CloseDrawingMode();
            return;
        }

        bool isDrawing = drawActionReference != null && drawActionReference.action.IsPressed();

        if (isDrawing && activeBallotTexture != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mouseScreenPos, null, out Vector2 localPoint))
            {
                Rect rect = rectTransform.rect;
                float normalizedX = (localPoint.x - rect.x) / rect.width;
                float normalizedY = (localPoint.y - rect.y) / rect.height;

                if (normalizedX >= 0f && normalizedX <= 1f && normalizedY >= 0f && normalizedY <= 1f)
                {
                    int texWidth = activeBallotTexture.width;
                    int texHeight = activeBallotTexture.height;

                    int pixelX = Mathf.RoundToInt(normalizedX * texWidth);
                    int pixelY = Mathf.RoundToInt(normalizedY * texHeight);
                    Vector2 currentPixelPos = new Vector2(pixelX, pixelY);

                    if (wasDrawingLastFrame)
                    {
                        DrawLine((int)lastDrawPixelPos.x, (int)lastDrawPixelPos.y, (int)currentPixelPos.x, (int)currentPixelPos.y, brushSize, penColor);
                    }
                    else
                    {
                        DrawBrush(pixelX, pixelY, brushSize, penColor);
                    }

                    lastDrawPixelPos = currentPixelPos;
                    wasDrawingLastFrame = true;
                    activeBallotTexture.Apply(); // Обновляет текстуру и в интерфейсе, и на 3D-модели в мире!
                }
                else
                {
                    wasDrawingLastFrame = false;
                }
            }
            else
            {
                wasDrawingLastFrame = false;
            }
        }
        else
        {
            wasDrawingLastFrame = false;
        }
    }

    private void DrawBrush(int x, int y, int size, Color color)
    {
        int texWidth = activeBallotTexture.width;
        int texHeight = activeBallotTexture.height;

        for (int i = -size; i <= size; i++)
        {
            for (int j = -size; j <= size; j++)
            {
                if (i * i + j * j <= size * size)
                {
                    int px = x + i;
                    int py = y + j;
                    if (px >= 0 && px < texWidth && py >= 0 && py < texHeight)
                    {
                        activeBallotTexture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    private void DrawLine(int x0, int y0, int x1, int y1, int size, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawBrush(x0, y0, size, color);

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void CloseDrawingMode()
    {
        if (playerController != null)
        {
            playerController.isDrawingMode = false;
            playerController.LockCursor();
        }
        gameObject.SetActive(false);
    }
}