using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterCreationController : MonoBehaviour
{
    [Header("UI Inputs")]
    [SerializeField] private TMP_InputField lastNameInputField;
    [SerializeField] private TMP_InputField firstNameInputField;
    [SerializeField] private TMP_Dropdown sexDropdown;           // 0 - Мужской (M), 1 - Женский (F)
    [SerializeField] private Button submitButton;                // Кнопка "Готово"

    [Header("Male Options (Мужские пресеты)")]
    [SerializeField] private GameObject[] malePrefabs;
    [SerializeField] private Sprite[] malePortraits;

    [Header("Female Options (Женские пресеты)")]
    [SerializeField] private GameObject[] femalePrefabs;
    [SerializeField] private Sprite[] femalePortraits;

    [Header("Settings")]
    [SerializeField] private Transform modelSpawnPoint;          // Точка спавна 3D модели
    [SerializeField] private string gameSceneName = "GameScene";

    private int currentModelIndex = 0;
    private GameObject currentSpawnedModel;                      // Текущая модель на сцене

    private void Start()
    {
        // Подписываемся на изменение пола в Dropdown
        if (sexDropdown != null)
        {
            sexDropdown.onValueChanged.AddListener(delegate { OnSexChanged(); });
        }

        // Подписываемся на изменение текста в полях ввода имени и фамилии
        if (firstNameInputField != null)
        {
            firstNameInputField.onValueChanged.AddListener(delegate { ValidateInputs(); });
        }
        if (lastNameInputField != null)
        {
            lastNameInputField.onValueChanged.AddListener(delegate { ValidateInputs(); });
        }

        UpdateCharacterPreview();
        ValidateInputs(); // Проверяем состояние кнопки при старте игры
    }

    // Вызывается при смене пола в выпадающем списке
    private void OnSexChanged()
    {
        currentModelIndex = 0; // Сбрасываем индекс, чтобы не выйти за рамки массива другого пола
        UpdateCharacterPreview();
    }

    // Проверка заполненности имени и фамилии
    private void ValidateInputs()
    {
        if (submitButton == null) return;

        bool isFirstNameValid = firstNameInputField != null && !string.IsNullOrWhiteSpace(firstNameInputField.text);
        bool isLastNameValid = lastNameInputField != null && !string.IsNullOrWhiteSpace(lastNameInputField.text);

        // Кнопка активна только если заполнены и имя, и фамилия
        submitButton.interactable = isFirstNameValid && isLastNameValid;
    }

    private bool IsMale()
    {
        if (sexDropdown == null) return true;
        return sexDropdown.value == 0; // 0 — Мужской, 1 — Женский
    }

    // Получаем актуальный массив префабов в зависимости от выбранного пола
    private GameObject[] GetActivePrefabs()
    {
        return IsMale() ? malePrefabs : femalePrefabs;
    }

    // Получаем актуальный массив портретов в зависимости от выбранного пола
    private Sprite[] GetActivePortraits()
    {
        return IsMale() ? malePortraits : femalePortraits;
    }

    // Кнопка "Следующий персонаж" (стрелочка вправо)
    public void NextModel()
    {
        GameObject[] prefabs = GetActivePrefabs();
        if (prefabs == null || prefabs.Length == 0) return;

        currentModelIndex = (currentModelIndex + 1) % prefabs.Length;
        UpdateCharacterPreview();
    }

    // Кнопка "Предыдущий персонаж" (стрелочка влево)
    public void PreviousModel()
    {
        GameObject[] prefabs = GetActivePrefabs();
        if (prefabs == null || prefabs.Length == 0) return;

        currentModelIndex = (currentModelIndex - 1 + prefabs.Length) % prefabs.Length;
        UpdateCharacterPreview();
    }

    // Спавн и обновление 3D-модели на сцене
    private void UpdateCharacterPreview()
    {
        if (currentSpawnedModel != null)
        {
            Destroy(currentSpawnedModel);
        }

        GameObject[] prefabs = GetActivePrefabs();
        if (prefabs != null && prefabs.Length > 0 && prefabs[currentModelIndex] != null)
        {
            GameObject prefabToSpawn = prefabs[currentModelIndex];

            if (modelSpawnPoint != null)
            {
                currentSpawnedModel = Instantiate(prefabToSpawn, modelSpawnPoint.position, modelSpawnPoint.rotation);
                currentSpawnedModel.transform.SetParent(modelSpawnPoint);
            }
            else
            {
                currentSpawnedModel = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);
            }
        }
    }

    // Кнопка "Готово"
    public void SubmitCharacter()
    {
        // Дополнительная проверка на случай попытки вызова
        string fName = firstNameInputField != null ? firstNameInputField.text.Trim() : "";
        string lName = lastNameInputField != null ? lastNameInputField.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(fName) || string.IsNullOrWhiteSpace(lName)) return;

        string sex = IsMale() ? "M" : "F";

        Sprite[] portraits = GetActivePortraits();
        Sprite chosenPortrait = (portraits != null && portraits.Length > currentModelIndex)
            ? portraits[currentModelIndex]
            : null;

        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.SetCharacterData(fName, lName, sex, currentModelIndex, chosenPortrait);
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
