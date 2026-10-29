using UnityEngine;
using System;

public static class PassportGenerator
{
    // Генерация номера паспорта РБ: 2 буквы + 7 цифр (например, AB1234567)
    public static string GeneratePassportNumber()
    {
        string letters = "ABCEHKMPRTX"; // Буквы, используемые в паспортах РБ
        char l1 = letters[UnityEngine.Random.Range(0, letters.Length)];
        char l2 = letters[UnityEngine.Random.Range(0, letters.Length)];
        int digits = UnityEngine.Random.Range(1000000, 10000000);
        return $"{l1}{l2}{digits}";
    }

    // Генерация идентификационного номера РБ (структура: XXXXXXX_XXX_XXXX)
    // Пример: 3 120585 A 001 PB 4
    public static string GenerateIdNumber()
    {
        int region = UnityEngine.Random.Range(1, 5); // Код области/Минска
        string birthPart = "120585"; // Упрощенно для примера
        string middle = "A" + UnityEngine.Random.Range(100, 1000).ToString();
        string end = "PB" + UnityEngine.Random.Range(0, 10).ToString();
        return $"{region}{birthPart}{middle}{end}";
    }

    // Генерация даты выдачи (через 18 лет после рождения) и срока действия (+10 лет)
    public static void GenerateDates(string dobStr, out string issueDateStr, out string expiryDateStr)
    {
        if (!DateTime.TryParse(dobStr, out DateTime dob))
        {
            dob = new DateTime(1990, 1, 1);
        }

        // Выдача паспорта, например, в день 18летия или случайный год позже
        DateTime issueDate = dob.AddYears(18).AddDays(UnityEngine.Random.Range(0, 300));
        DateTime expiryDate = issueDate.AddYears(10);

        issueDateStr = issueDate.ToString("dd.MM.yyyy");
        expiryDateStr = expiryDate.ToString("dd.MM.yyyy");
    }
}
