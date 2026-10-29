using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    [Header("Player Input (Заглушки)")]
    public string firstName = "JOHN";
    public string lastName = "CITIZEN";
    public string sex = "M";
    public string dateOfBirth = "12.05.1985"; // ДД.ММ.ГГГГ

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
}
