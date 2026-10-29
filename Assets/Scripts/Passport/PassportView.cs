using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassportView : MonoBehaviour
{
    [Header("UI References (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI typeText;           // P
    [SerializeField] private TextMeshProUGUI codeText;           // BLR
    [SerializeField] private TextMeshProUGUI passportNumberText; // Номер паспорта
    [SerializeField] private TextMeshProUGUI surnameText;        // Фамилия
    [SerializeField] private TextMeshProUGUI givenNamesText;     // Имя
    [SerializeField] private TextMeshProUGUI nationalityText;    // REPUBLIC OF BELARUS
    [SerializeField] private TextMeshProUGUI dobText;            // Дата рождения
    [SerializeField] private TextMeshProUGUI idNumberText;       // Идентификационный номер
    [SerializeField] private TextMeshProUGUI sexText;            // Пол (M/F)
    [SerializeField] private TextMeshProUGUI pobText;            // Место рождения
    [SerializeField] private TextMeshProUGUI issueDateText;      // Дата выдачи
    [SerializeField] private TextMeshProUGUI authorityText;      // MINISTRY OF INTERNAL AFFAIRS
    [SerializeField] private TextMeshProUGUI expiryDateText;     // Срок действия

    [Header("Photo & MRZ")]
    [SerializeField] private Image photoImage;                   // Фото игрока (заглушка)
    [SerializeField] private TextMeshProUGUI mrzText;            // Машиночитаемая зона внизу

    [Header("Settings")]
    [SerializeField] private Sprite defaultPlayerPhoto;          // Заглушка фото (например, Мистер Бин из примера)

    private void Start()
    {
        FillPassportData();
    }

    private void FillPassportData()
    {
        // Получаем данные игрока из менеджера
        PlayerData player = PlayerData.Instance;
        string fName = player != null ? player.firstName : "JOHN";
        string lName = player != null ? player.lastName : "CITIZEN";
        string sex = player != null ? player.sex : "M";
        string dob = player != null ? player.dateOfBirth : "01.01.1990";

        // Генерация переменных данных
        string passNum = PassportGenerator.GeneratePassportNumber();
        string idNum = PassportGenerator.GenerateIdNumber();
        PassportGenerator.GenerateDates(dob, out string issueDate, out string expiryDate);

        // Заполняем текстовые поля
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

        // Устанавливаем фото (если есть)
        if (photoImage && defaultPlayerPhoto)
        {
            photoImage.sprite = defaultPlayerPhoto;
        }

        // Генерация машиночитаемой зоны (MRZ) в стиле белорусского паспорта
        if (mrzText)
        {
            string mrzLine1 = $"P<BLR{lName.ToUpper()}<<{fName.ToUpper()}<<<<<<<<<<<<<<<<<<<<<<";
            string mrzLine2 = $"{passNum}0BLR{dob.Replace(".", "")}0{sex}{expiryDate.Replace(".", "")}0{idNum}<<<<<04";
            mrzText.text = $"{mrzLine1}\n{mrzLine2}";
        }
    }
}
