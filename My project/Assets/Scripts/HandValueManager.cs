using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandValueManager : MonoBehaviour
{
    public static HandValueManager Instance { get; private set; }

    [Header("Base Scores")]
    public float highCardBase;
    public float onePairBase;
    public float twoPairBase;
    public float threeKindBase;
    public float straightBase;
    public float flushBase;
    public float fullHouseBase;
    public float fourKindBase;
    public float straightFlushBase;

    [Header("Combo Bonuses")]
    public float highCardComboScore;
    public float onePairComboScore;
    public float twoPairComboScore;
    public float threeKindComboScore;
    public float straightComboScore;
    public float flushComboScore;
    public float fullHouseComboScore;
    public float fourKindComboScore;
    public float straightFlushComboScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }
}
