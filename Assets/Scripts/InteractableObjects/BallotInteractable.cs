using UnityEngine;
using UnityEngine.UI;

public class BallotInteractable : MonoBehaviour, IInteractable
{
    [Header("UI References")]
    [SerializeField] private GameObject ballotDrawingScreenUI; // Панель рисования на экране
    [SerializeField] private RawImage worldBallotRawImage; // RawImage на Canvas в 3D мире (над столом)

    [Header("Template Texture")]
    [SerializeField] private Texture2D ballotTemplate; // Исходный макет бюллетеня (Read/Write Enabled)

    private Texture2D sharedBallotTexture;

    private void Start()
    {
        if (ballotTemplate != null && worldBallotRawImage != null)
        {
            // Создаем единую дублированную текстуру для рисования
            sharedBallotTexture = new Texture2D(ballotTemplate.width, ballotTemplate.height, TextureFormat.RGBA32, false);
            sharedBallotTexture.SetPixels(ballotTemplate.GetPixels());
            sharedBallotTexture.Apply();

            // Назначаем текстуру на World Space Canvas над столом
            worldBallotRawImage.texture = sharedBallotTexture;
        }
        else
        {
            Debug.LogWarning("Заполните World Ballot Raw Image и Ballot Template в BallotInteractable!");
        }
    }

    public string GetInteractPrompt()
    {
        return "[E] Заполнить бюллетень";
    }

    public void Interact(PlayerController player)
    {
        if (ballotDrawingScreenUI != null && sharedBallotTexture != null)
        {
            ballotDrawingScreenUI.SetActive(true);

            // Передаем ту же самую текстуру в Screen Space UI
            BallotCanvasDrawer drawer = ballotDrawingScreenUI.GetComponent<BallotCanvasDrawer>();
            if (drawer != null)
            {
                drawer.InitWithTexture(sharedBallotTexture);
            }

            player.isDrawingMode = true;
            player.UnlockCursor();
            if (player.interactionPromptText != null) player.interactionPromptText.text = "";
        }
    }
}