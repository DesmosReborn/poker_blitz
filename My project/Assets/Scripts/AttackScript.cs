using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;

public class AttackScript : MonoBehaviourPun
{
    public List<CardScript> cards;
    public float power;
    public float currPower;
    private DeckScript deck;
    private PlayerScript player;
    [SerializeField] private bool attackedThisFrame = false;

    [SerializeField] float highCardMult;
    [SerializeField] float onePairMult;
    [SerializeField] float twoPairMult;
    [SerializeField] float threeKindMult;
    [SerializeField] float straightMult;
    [SerializeField] float flushMult;
    [SerializeField] float fullHouseMult;
    [SerializeField] float fourKindMult;
    [SerializeField] float straightFlushMult;

    [SerializeField] float highCardBase;
    [SerializeField] float onePairBase;
    [SerializeField] float twoPairBase;
    [SerializeField] float threeKindBase;
    [SerializeField] float straightBase;
    [SerializeField] float flushBase;
    [SerializeField] float fullHouseBase;
    [SerializeField] float fourKindBase;
    [SerializeField] float straightFlushBase;

    [SerializeField] float highCardComboScore;
    [SerializeField] float onePairComboScore;
    [SerializeField] float twoPairComboScore;
    [SerializeField] float threeKindComboScore;
    [SerializeField] float straightComboScore;
    [SerializeField] float flushComboScore;
    [SerializeField] float fullHouseComboScore;
    [SerializeField] float fourKindComboScore;
    [SerializeField] float straightFlushComboScore;

    [SerializeField] float baseMoveSpeed;
    [SerializeField] float initialMoveSpeed;
    [SerializeField] float decayRate;
    private float moveSpeed;

    [SerializeField] GameObject cardSpawnVFX;
    [SerializeField] TextMeshProUGUI dmgUI;

    [SerializeField] private PhotonView view;

    [PunRPC]
    private void RPC_InitializeAttack(int[] cardViewIDs, int playerViewID)
    {
        Start();
        PhotonView playerView = PhotonView.Find(playerViewID);
        if (playerView == null)
        {
            Debug.LogWarning("Player PhotonView not found: " + playerViewID);
            return;
        }

        player = playerView.GetComponent<PlayerScript>();
        if (player == null)
        {
            Debug.LogWarning("Player missing on view ID: " + playerViewID);
            return;
        }
        deck = player.GetComponentInChildren<DeckScript>();
        this.transform.SetParent(player.transform, false);

        for (int i = 0; i < cardViewIDs.Length; i++)
        {
            PhotonView cardView = PhotonView.Find(cardViewIDs[i]);
            if (cardView == null)
            {
                Debug.LogWarning("Card PhotonView not found: " + cardViewIDs[i]);
                return;
            }

            CardScript card = cardView.GetComponent<CardScript>();
            if (card == null)
            {
                Debug.LogWarning("CardScript missing on view ID: " + cardViewIDs[i]);
                return;
            }
            cards.Add(card);
        }

        power = calculateHandStrength() * player.currComboMult;
        currPower = power;
        dmgUI.text = currPower.ToString();
        if (!playerView.IsMine)
        {
            dmgUI.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }
        foreach (CardScript card in cards)
        {
            card.selected = false;
            float prevX = card.transform.localPosition.x;
            card.transform.SetParent(this.transform, true);
            card.transform.localPosition = new Vector3(prevX, 0, 0);
            float endRotationY = card.transform.localEulerAngles.y + 720;
            card.invokeCardAnim(endRotationY, card.transform.localPosition, card.defaultScale, card.spawnTime * 2);
            card.col.enabled = true;
        }
    }

    [PunRPC]
    private void RPC_DestroyAttack()
    {
        moveSpeed = 0;
        foreach (CardScript card in cards)
        {
            GameObject vfx = Instantiate(cardSpawnVFX);
            vfx.transform.position = card.transform.position;
            float endRotationY = card.transform.localEulerAngles.y + 360;
            Vector3 endPosition = card.transform.localPosition;
            Vector3 endScale = new Vector3(0, card.defaultScale.y, card.defaultScale.z);
            card.invokeCardAnim(endRotationY, endPosition, Vector3.zero, card.spawnTime * 2);
            card.transform.SetParent(deck.transform, true);
            card.transform.localPosition = Vector3.zero;
            card.col.enabled = false;
            deck.played.Add(card);
        }
        StartCoroutine(DestroyAfterDelay(0.2f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

    private void DestroyAttack()
    {
        Debug.Log("Destroying Attack with ID " + photonView.ViewID);
        foreach (CardScript card in cards)
        {
            GameObject vfx = Instantiate(cardSpawnVFX);
            vfx.transform.position = card.transform.position;
            float endRotationY = card.transform.localEulerAngles.y + 360;
            Vector3 endPosition = card.transform.localPosition;
            Vector3 endScale = new Vector3(0, card.defaultScale.y, card.defaultScale.z);
            card.invokeCardAnim(endRotationY, endPosition, Vector3.zero, card.spawnTime * 2);
            card.transform.SetParent(deck.transform, true);
            card.transform.localPosition = Vector3.zero;
            card.col.enabled = false;
            deck.played.Add(card);
        }
        Destroy(this.gameObject, 0.2f);
    }

    // Start is called before the first frame update
    void Start()
    {
        moveSpeed = initialMoveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return; // only owner moves/updates attack

        moveSpeed = Mathf.Clamp(moveSpeed - decayRate * Time.deltaTime, baseMoveSpeed, initialMoveSpeed);
        attackedThisFrame = false;
        move();
    }

    private float calculateHandStrength()
    {
        float handTypeMultiplier = getHandTypeMultiplier();
        float handStrength = 0;
        foreach (CardScript card in cards)
        {
            handStrength += card.value;
        }
        return Mathf.Floor(getHandTypeBase() + handStrength / cards.Count);
    }

    private float getHandTypeMultiplier()
    {
        List<float> ranks = new List<float>();
        List<string> suits = new List<string>();
        int blackJokerCount = 0;
        int redJokerCount = 0;

        foreach (CardScript card in cards)
        {
            if (card.value == 15)
            {
                if (card.suit == "red") redJokerCount++;
                if (card.suit == "black") blackJokerCount++;
            }
            else
            {
                ranks.Add(card.value);
                suits.Add(card.suit);
            }
        }

        ranks.Sort();

        Dictionary<float, int> rankCounts = new Dictionary<float, int>();
        foreach (float rank in ranks)
        {
            if (rankCounts.ContainsKey(rank))
                rankCounts[rank]++;
            else
                rankCounts[rank] = 1;
        }

        // Check for a flush (all suits the same)
        bool isFlush = false;

        // Check for a straight
        bool isStraight = false;

        if (cards.Count == 5)
        {
            var suitGroups = suits.GroupBy(suit => suit).ToDictionary(g => g.Key, g => g.Count());
            string bestFlushSuit = suitGroups.OrderByDescending(g => g.Value).FirstOrDefault().Key;
            int flushCount = suitGroups.ContainsKey(bestFlushSuit) ? suitGroups[bestFlushSuit] : 0;

            flushCount += (blackJokerCount + redJokerCount);
            isFlush = flushCount >= 5;

            int gaps = 0;
            for (int i = 1; i < ranks.Count; i++)
            {
                float diff = ranks[i] - ranks[i - 1];
                if (Mathf.Abs(diff - 1) > 0.01f)
                {
                    gaps += (int)(diff - 1);
                }
            }
            isStraight = (gaps <= (blackJokerCount + redJokerCount));
        }

        while (blackJokerCount > 0 || redJokerCount > 0)
        {
            if (rankCounts.Count > 0)
            {
                float mostCommonRank = rankCounts.OrderByDescending(kvp => kvp.Value).First().Key;
                rankCounts[mostCommonRank]++;
            }
            else
            {
                rankCounts[15f] = blackJokerCount + redJokerCount;
            }

            if (blackJokerCount > 0) blackJokerCount--;
            else if (redJokerCount > 0) redJokerCount--;
        }

        // Find the highest count of any rank
        int maxCount = rankCounts.Values.Max();

        if (isFlush && isStraight)
        { 
            player.updateComboScore(straightFlushComboScore);
            return straightFlushMult;
        } // Straight Flush
        if (maxCount == 4) 
        { 
            player.updateComboScore(fourKindComboScore);
            return fourKindMult;
        } // Four of a Kind
        if (maxCount == 3 && rankCounts.Count == 2) 
        {
            player.updateComboScore(fullHouseComboScore);
            return fullHouseMult;
        } // Full House
        if (isFlush)
        {
            player.updateComboScore(flushComboScore);
            return flushMult;
        } // Flush
        if (isStraight)
        {
            player.updateComboScore(straightComboScore);
            return straightMult;
        } // Straight
        if (maxCount == 3)
        { 
            player.updateComboScore(threeKindComboScore);
            return threeKindMult; 
        } // Three of a Kind
        if (maxCount == 2 && rankCounts.Count <= cards.Count - 2) 
        { 
            player.updateComboScore(twoPairComboScore);
            return twoPairMult;
        } // Two Pair
        if (maxCount == 2) 
        {
            player.updateComboScore(onePairComboScore);;
            return onePairMult;
        } // One Pair
        player.updateComboScore(highCardComboScore);
        return highCardMult;// High Card
    }

    private float getHandTypeBase()
    {
        List<float> ranks = new List<float>();
        List<string> suits = new List<string>();
        int blackJokerCount = 0;
        int redJokerCount = 0;

        foreach (CardScript card in cards)
        {
            if (card.value == 15)
            {
                if (card.suit == "red") redJokerCount++;
                if (card.suit == "black") blackJokerCount++;
            }
            else
            {
                ranks.Add(card.value);
                suits.Add(card.suit);
            }
        }

        ranks.Sort();

        Dictionary<float, int> rankCounts = new Dictionary<float, int>();
        foreach (float rank in ranks)
        {
            if (rankCounts.ContainsKey(rank))
                rankCounts[rank]++;
            else
                rankCounts[rank] = 1;
        }

        // Check for a flush (all suits the same)
        bool isFlush = false;

        // Check for a straight
        bool isStraight = false;

        if (cards.Count == 5)
        {
            var suitGroups = suits.GroupBy(suit => suit).ToDictionary(g => g.Key, g => g.Count());
            string bestFlushSuit = suitGroups.OrderByDescending(g => g.Value).FirstOrDefault().Key;
            int flushCount = suitGroups.ContainsKey(bestFlushSuit) ? suitGroups[bestFlushSuit] : 0;

            flushCount += (blackJokerCount + redJokerCount);
            isFlush = flushCount >= 5;

            int gaps = 0;
            for (int i = 1; i < ranks.Count; i++)
            {
                float diff = ranks[i] - ranks[i - 1];
                if (Mathf.Abs(diff - 1) > 0.01f)
                {
                    gaps += (int)(diff - 1);
                }
            }
            isStraight = (gaps <= (blackJokerCount + redJokerCount));
        }

        while (blackJokerCount > 0 || redJokerCount > 0)
        {
            if (rankCounts.Count > 0)
            {
                float mostCommonRank = rankCounts.OrderByDescending(kvp => kvp.Value).First().Key;
                rankCounts[mostCommonRank]++;
            }
            else
            {
                rankCounts[15f] = blackJokerCount + redJokerCount;
            }

            if (blackJokerCount > 0) blackJokerCount--;
            else if (redJokerCount > 0) redJokerCount--;
        }

        // Find the highest count of any rank
        int maxCount = rankCounts.Values.Max();

        if (isFlush && isStraight)
        {
            player.updateComboScore(straightFlushComboScore);
            return straightFlushBase;
        } // Straight Flush
        if (maxCount == 4)
        {
            player.updateComboScore(fourKindComboScore);
            return fourKindBase;
        } // Four of a Kind
        if (maxCount == 3 && rankCounts.Count == 2)
        {
            player.updateComboScore(fullHouseComboScore);
            return fullHouseBase;
        } // Full House
        if (isFlush)
        {
            player.updateComboScore(flushComboScore);
            return flushBase;
        } // Flush
        if (isStraight)
        {
            player.updateComboScore(straightComboScore);
            return straightBase;
        } // Straight
        if (maxCount == 3)
        {
            player.updateComboScore(threeKindComboScore);
            return threeKindBase;
        } // Three of a Kind
        if (maxCount == 2 && rankCounts.Count <= cards.Count - 2)
        {
            player.updateComboScore(twoPairComboScore);
            return twoPairBase;
        } // Two Pair
        if (maxCount == 2)
        {
            player.updateComboScore(onePairComboScore); ;
            return onePairBase;
        } // One Pair
        player.updateComboScore(highCardComboScore);
        return highCardBase;// High Card
    }

    private void move()
    {
        this.transform.position += this.transform.up * moveSpeed * Time.deltaTime;
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (!photonView.IsMine) return;
        if (!attackedThisFrame)
        {
            Debug.Log("Attack with ID " + photonView.ViewID + " Taking " + damage + " Damage");
            currPower -= damage;
            dmgUI.text = currPower.ToString();
            attackedThisFrame = true;
            if (currPower <= 0)
            {
                DestroyAttack();
            }
        }
    }

    [PunRPC]
    public void RPC_UpdatePower(float newPower)
    {
        currPower = newPower;
        dmgUI.text = currPower.ToString();
    }
}
