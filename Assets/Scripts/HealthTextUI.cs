using TMPro;
using UnityEngine;

public class HealthTextUI : MonoBehaviour
{
    public TextMeshProUGUI healthText;

    public void SetHealth(int health)
    {
        healthText.text = "Health: " + health;
    }
}
