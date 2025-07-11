﻿using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using Photon.Pun;
public class PlayerScript : MonoBehaviourPun
{
    private GameManager gm;
    [SerializeField] private HandScript hand;
    [SerializeField] float maxHP = 1000;
    private float currHP;
    [SerializeField] float playCD = 1.0f;
    private float currPlayCD;

    [SerializeField] float comboScore;
    [SerializeField] float comboFThreshold;
    [SerializeField] float comboEThreshold;
    [SerializeField] float comboDThreshold;
    [SerializeField] float comboCThreshold;
    [SerializeField] float comboBThreshold;
    [SerializeField] float comboAThreshold;
    [SerializeField] float comboSThreshold;
    [SerializeField] float comboSSThreshold;
    [SerializeField] float comboSSSThreshold;
    private List<float> comboThresholds;

    [SerializeField] float comboFMult;
    [SerializeField] float comboEMult;
    [SerializeField] float comboDMult;
    [SerializeField] float comboCMult;
    [SerializeField] float comboBMult;
    [SerializeField] float comboAMult;
    [SerializeField] float comboSMult;
    [SerializeField] float comboSSMult;
    [SerializeField] float comboSSSMult;
    private List<float> comboMults;
    public float currComboMult;

    private StyleMeterManager smm;

    private List<string> comboStrings = new List<string>() { "F", "E", "D", "C", "B", "A", "S", "SS", "SSS" };
    public string currComboString;

    [SerializeField] float comboScoreFlatDecrease;
    [SerializeField] float comboScorePercentDecrease;
    [SerializeField] float comboScoreDecreaseCD;
    private float currComboScoreDecreaseCD;

    public PhotonView view;
    [SerializeField] private Camera myCamera;
    private bool attackedThisFrame = false;

    private HealthbarScript healthbar;
    [SerializeField] private Canvas myUI;
    [SerializeField] public bool isAI = false;
    private AIPlayerScript ai;

    public void updateComboScore(float score)
    {
        comboScore = Mathf.Max(0, comboScore + score);

        int left = 0, right = comboThresholds.Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (comboThresholds[mid] < comboScore)
                left = mid + 1;
            else
                right = mid - 1;
        }

        currComboMult = comboMults[Mathf.Max(0, right)];
        currComboString = comboStrings[Mathf.Max(0, right)];

        if (!isAI) 
        {
            smm.updateStyleMeter(currComboString);
        }
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (!attackedThisFrame) {
            currHP -= damage;
            healthbar.SetHealth(currHP);
            attackedThisFrame = true;
            if (currHP <= 0)
            {
                gm.gameOver();
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        view = GetComponent<PhotonView>();
        gm = GameObject.FindObjectOfType<GameManager>();
        smm = GetComponent<StyleMeterManager>();
        comboThresholds = new List<float>() { comboFThreshold, comboEThreshold, comboDThreshold, comboCThreshold, comboBThreshold, comboAThreshold, comboSThreshold, comboSSThreshold, comboSSSThreshold };
        comboMults = new List<float>() { comboFMult, comboEMult, comboDMult, comboCMult, comboBMult, comboAMult, comboSMult, comboSSMult, comboSSSMult };
        currPlayCD = playCD;
        comboScore = 0;
        currComboMult = 1;
        currComboString = comboStrings[0];
        currComboScoreDecreaseCD = comboScoreDecreaseCD;
        currHP = maxHP;
        healthbar = GetComponent<HealthbarScript>();
        healthbar.Initialize(view);
        healthbar.SetMaxHealth(maxHP);

        if (view.IsMine && !isAI)
        {
            myCamera.gameObject.SetActive(true);
            myUI.gameObject.SetActive(true);
            smm.updateStyleMeter(currComboString);
        }
        if (isAI)
        {
            ai = GetComponent<AIPlayerScript>();

        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isAI)
        {
            if (currPlayCD <= 0)
            {
                isAIPlayHand();
                currPlayCD = playCD * 1.3f;
            }
            currPlayCD -= Time.deltaTime;
        }
        else
        {
            if (view.IsMine)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    if (hand.hand[0] != null)
                    {
                        hand.hand[0].toggleSelected();
                        hand.updateHandType();
                    }
                }
                if (Input.GetKeyDown(KeyCode.W))
                {
                    if (hand.hand[1] != null)
                    {
                        hand.hand[1].toggleSelected();
                        hand.updateHandType();
                    }
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (hand.hand[2] != null)
                    {
                        hand.hand[2].toggleSelected();
                        hand.updateHandType();
                    }
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    if (hand.hand[3] != null)
                    {
                        hand.hand[3].toggleSelected();
                        hand.updateHandType();
                    }
                }
                if (Input.GetKeyDown(KeyCode.T))
                {
                    if (hand.hand[4] != null)
                    {
                        hand.hand[4].toggleSelected();
                        hand.updateHandType();
                    }
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (hand.hand[4] != null)
                    {
                        hand.hand[4].toggleSelected();
                    }
                    if (hand.hand[3] != null)
                    {
                        hand.hand[3].toggleSelected();
                    }
                    if (hand.hand[2] != null)
                    {
                        hand.hand[2].toggleSelected();
                    }
                    if (hand.hand[1] != null)
                    {
                        hand.hand[1].toggleSelected();
                    }
                    if (hand.hand[0] != null)
                    {
                        hand.hand[0].toggleSelected();
                    }
                    hand.updateHandType();
                }
                if (Input.GetKeyDown(KeyCode.Space) && currPlayCD <= 0)
                {
                    PhotonView handView = hand.GetComponent<PhotonView>();
                    handView.RPC("RPC_PlayHand", RpcTarget.AllBuffered);
                    currPlayCD = playCD;
                }
                currPlayCD -= Time.deltaTime;
                currComboScoreDecreaseCD -= Time.deltaTime;
                if (currComboScoreDecreaseCD < 0)
                {
                    comboScore = Mathf.Max(0, comboScore * (1 - comboScorePercentDecrease) - comboScoreFlatDecrease);
                    smm.updateStyleMeter(currComboString);
                    currComboScoreDecreaseCD = comboScoreDecreaseCD;
                }
            }
        }
        attackedThisFrame = false;
    }

    private void isAIPlayHand()
    {
        ai.playHand();
    }
}
