using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    [Header("Player Personal Data")]
    public string firstName = "JOHN";
    public string lastName = "CITIZEN";
    public string sex = "M";
    public string dateOfBirth = "12.05.1985";

    [Header("Character Customization")]
    public int selectedModelIndex = 0;
    public Sprite selectedPortrait;

    [Header("Passport Unique Data (Persistent)")]
    public string passportNumber;
    public string idNumber;
    public string issueDate;
    public string expiryDate;
    private bool isPassportGenerated = false;   // Флаг, чтобы сгенерировать данные только 1 раз

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCharacterData(string fName, string lName, string selectedSex, int modelIndex, Sprite portrait)
    {
        firstName = string.IsNullOrWhiteSpace(fName) ? "JOHN" : fName.Trim();
        lastName = string.IsNullOrWhiteSpace(lName) ? "CITIZEN" : lName.Trim();
        sex = string.IsNullOrWhiteSpace(selectedSex) ? "M" : selectedSex.Trim();
        selectedModelIndex = modelIndex;
        selectedPortrait = portrait;

        // При создании персонажа сбрасываем генерацию паспорта, чтобы он сгенерировался заново под новые данные
        isPassportGenerated = false;
    }

    // Генерация уникальных данных паспорта один раз
    public void GeneratePassportDataIfNeeded()
    {
        if (isPassportGenerated) return;

        passportNumber = PassportGenerator.GeneratePassportNumber();
        idNumber = PassportGenerator.GenerateIdNumber();
        PassportGenerator.GenerateDates(dateOfBirth, out issueDate, out expiryDate);

        isPassportGenerated = true;
    }
}
