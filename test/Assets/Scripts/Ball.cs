using UnityEngine;
using TMPro;

public class Ball : MonoBehaviour
{
    public int number; // Номер шарика
    public TextMeshPro numberText; // Текст для отображения номера (используем TextMeshPro)

    public void SetNumber(int num)
    {
        number = num;
        numberText.text = num.ToString(); // Устанавливаем текст через TextMeshPro
    }
}