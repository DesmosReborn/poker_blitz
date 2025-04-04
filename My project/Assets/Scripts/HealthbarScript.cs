using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthbarScript : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthAmount;
    private float maxHealth;

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        healthSlider.maxValue = newMaxHealth;
        healthSlider.value = newMaxHealth;
        healthAmount.text = maxHealth.ToString() + "/" + maxHealth.ToString();
    }

    public void SetHealth(float health)
    {
        healthSlider.value = health;
        healthAmount.text = health.ToString() + "/" + maxHealth.ToString();
    }
}
