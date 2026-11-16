using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassportInspectUI : MonoBehaviour
{
    [Header("UI Panel Container")]
    [SerializeField] private GameObject inspectPanel; // Сама панель паспорта на экране

    [Header("UI References (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private TextMeshProUGUI passportNumberText;
    [SerializeField] private TextMeshProUGUI surnameText;
    [SerializeField] private TextMeshProUGUI givenNamesText;
    [SerializeField] private TextMeshProUGUI nationalityText;
    [SerializeField] private TextMeshProUGUI dobText;
    [SerializeField] private TextMeshProUGUI idNumberText;
    [SerializeField] private TextMeshProUGUI sexText;
    [SerializeField] private TextMeshProUGUI pobText;
    [SerializeField] private TextMeshProUGUI issueDateText;
    [SerializeField] private TextMeshProUGUI authorityText;
    [SerializeField] private TextMeshProUGUI expiryDateText;

    [Header("Photo & MRZ")]
    [SerializeField] private Image photoImage;
    [SerializeField] private TextMeshProUGUI mrzText;

    [Header("Settings")]
    [SerializeField] private Sprite defaultPlayerPhoto;

    private PlayerController playerController;
    private bool isOpen = false;

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        if (inspectPanel != null)
        {
            inspectPanel.SetActive(false); // При старте паспорт скрыт
        }
    }

    private void Update()
    {
        if (isOpen)
        {
            // Проверяем действие Close из Input System (в нем можно настроить и ПКМ, и Escape)
            if (playerController != null && playerController.closeAction != null)
            {
                if (playerController.closeAction.action.WasPressedThisFrame())
                {
                    CloseInspection();
                }
            }
        }
    }

    public void OpenInspection()
    {
        if (inspectPanel == null) return;

        FillPassportData();
        inspectPanel.SetActive(true);
        isOpen = true;

        if (playerController != null)
        {
            playerController.isDrawingMode = true;
            playerController.UnlockCursor();

            if (playerController.interactionPromptText != null)
            {
                playerController.interactionPromptText.text = "";
            }
        }
    }

    public void CloseInspection()
    {
        if (inspectPanel == null) return;

        inspectPanel.SetActive(false);
        isOpen = false;

        if (playerController != null)
        {
            playerController.isDrawingMode = false;
            playerController.LockCursor();
        }
    }

    private void FillPassportData()
    {
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.GeneratePassportDataIfNeeded();
        }

        PlayerData player = PlayerData.Instance;
        string fName = player != null ? player.firstName : "JOHN";
        string lName = player != null ? player.lastName : "CITIZEN";
        string sex = player != null ? player.sex : "M";
        string dob = player != null ? player.dateOfBirth : "01.01.1990";
        Sprite playerPortrait = player != null ? player.selectedPortrait : null;

        string passNum = player != null ? player.passportNumber : "AB0000000";
        string idNum = player != null ? player.idNumber : "0000000A000AA";
        string issueDate = player != null ? player.issueDate : "01.01.2005";
        string expiryDate = player != null ? player.expiryDate : "01.01.2015";

        if (typeText) typeText.text = "P";
        if (codeText) codeText.text = "BLR";
        if (passportNumberText) passportNumberText.text = passNum;
        if (surnameText) surnameText.text = lName;
        if (givenNamesText) givenNamesText.text = fName;
        if (nationalityText) nationalityText.text = "REPUBLIC OF BELARUS";
        if (dobText) dobText.text = dob;
        if (idNumberText) idNumberText.text = idNum;
        if (sexText) sexText.text = sex;
        if (pobText) pobText.text = "REPUBLIC OF BELARUS";
        if (issueDateText) issueDateText.text = issueDate;
        if (authorityText) authorityText.text = "MINISTRY OF INTERNAL AFFAIRS";
        if (expiryDateText) expiryDateText.text = expiryDate;

        if (photoImage && playerPortrait != null)
        {
            photoImage.sprite = playerPortrait;
        }
        else if (photoImage && defaultPlayerPhoto != null)
        {
            photoImage.sprite = defaultPlayerPhoto;
        }

        if (mrzText)
        {
            string mrzLine1 = $"P<BLR{lName.ToUpper()}<<{fName.ToUpper()}<<<<<<<<<<<<<<<<<<<<<<";
            string mrzLine2 = $"{passNum}0BLR{dob.Replace(".", "")}0{sex}{expiryDate.Replace(".", "")}0{idNum}<<<<<04";
            mrzText.text = $"{mrzLine1}\n{mrzLine2}";
        }
    }
}
