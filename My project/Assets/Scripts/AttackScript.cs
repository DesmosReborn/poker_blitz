using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class AttackScript : MonoBehaviour
{
    public List<CardScript> cards;
    public float power;
    public float currPower;
    private DeckScript deck;
    private PlayerScript player;
    private bool attackedThisFrame = false;

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

    public void Initialize(List<CardScript> newCards)
    {
        Start();
        cards = newCards;
        power = calculateHandStrength() * player.currComboMult;
        currPower = power;
        dmgUI.text = currPower.ToString();
        foreach (CardScript card in cards)
        {
            card.selected = false;
            float prevX = card.transform.localPosition.x;
            card.transform.SetParent(this.transform, true);
            card.transform.localPosition = new Vector3(prevX, 0, 0);
            float endRotationY = card.transform.localEulerAngles.y + 720;
            card.invokeCardAnim(endRotationY, card.transform.localPosition, card.defaultScale, card.spawnTime * 2);
        }
    }

    private void DestroyAttack()
    {
        foreach (CardScript card in cards)
        {
            GameObject vfx = Instantiate(cardSpawnVFX);
            vfx.transform.position = card.transform.position;
            float endRotationY = card.transform.localEulerAngles.y + 360;
            Vector3 endPosition = card.transform.localPosition;
            Vector3 endScale = new Vector3(0, card.defaultScale.y, card.defaultScale.z);
            card.invokeCardAnim(endRotationY, endPosition, Vector3.zero, card.spawnTime * 2);
            card.transform.SetParent(deck.transform, true);
            deck.played.Add(card);
        }
        Destroy(this.gameObject, 0.2f);
    }

    // Start is called before the first frame update
    void Start()
    {
        player = this.transform.parent.GetComponent<PlayerScript>();
        deck = player.GetComponentInChildren<DeckScript>();
        moveSpeed = initialMoveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
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
        return getHandTypeBase() + handStrength;
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
        this.transform.localPosition += this.transform.up * moveSpeed * Time.deltaTime;
    }

    public void takeDamage(float damage)
    {
        if (!attackedThisFrame)
        {
            currPower -= damage;
            dmgUI.text = currPower.ToString();
            attackedThisFrame = true;
            if (currPower <= 0)
            {
                DestroyAttack();
            }
        }
    }
}
