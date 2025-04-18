using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Photon.Pun;

public class HealthbarScript : MonoBehaviourPun
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthAmount;
    private float maxHealth;

    public void Initialize(PhotonView playerView)
    {
        if (!playerView.IsMine)
        {
            healthAmount.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }
    }

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
