using UnityEngine;
using TMPro;

public class Ball : MonoBehaviour
{
    public int number; // Номер шарика
    public TextMeshPro numberText; // Текст для отображения номера

    public void SetNumber(int num)
    {
        number = num;

        // Устанавливаем текст в зависимости от номера шарика
        switch (number)
        {
            case 0: // Бонусный шарик на время
                numberText.text = "+5"; // Добавляет 5 секунд
                break;
            case -2: // Бонусный шарик на жизнь
                numberText.text = "+1"; // Добавляет 1 жизнь
                break;
            default: // Обычный шарик
                numberText.text = num.ToString(); // Отображаем номер
                break;
        }
    }
}