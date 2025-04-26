using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class AIPlayerScript : MonoBehaviour
{
    private PlayerScript player;
    [SerializeField] private HandScript hand;
    [SerializeField] private DeckScript deck;

    private float highCardBase;
    private float onePairBase;
    private float twoPairBase;
    private float threeKindBase;
    private float straightBase;
    private float flushBase;
    private float fullHouseBase;
    private float fourKindBase;
    private float straightFlushBase;

    private float highCardComboScore;
    private float onePairComboScore;
    private float twoPairComboScore;
    private float threeKindComboScore;
    private float straightComboScore;
    private float flushComboScore;
    private float fullHouseComboScore;
    private float fourKindComboScore;
    private float straightFlushComboScore;

    private void Start()
    {
        player = GetComponent<PlayerScript>();

        highCardBase = HandValueManager.Instance.highCardBase;
        onePairBase = HandValueManager.Instance.onePairBase;
        twoPairBase = HandValueManager.Instance.twoPairBase;
        threeKindBase = HandValueManager.Instance.threeKindBase;
        straightBase = HandValueManager.Instance.straightBase;
        flushBase = HandValueManager.Instance.flushBase;
        fullHouseBase = HandValueManager.Instance.fullHouseBase;
        fourKindBase = HandValueManager.Instance.fourKindBase;
        straightFlushBase = HandValueManager.Instance.straightFlushBase;
        highCardComboScore = HandValueManager.Instance.highCardComboScore;
        onePairComboScore = HandValueManager.Instance.onePairComboScore;
        twoPairComboScore = HandValueManager.Instance.twoPairComboScore;
        threeKindComboScore = HandValueManager.Instance.threeKindComboScore;
        straightComboScore = HandValueManager.Instance.straightComboScore;
        flushComboScore = HandValueManager.Instance.flushComboScore;
        fullHouseComboScore = HandValueManager.Instance.fullHouseComboScore;
        fourKindComboScore = HandValueManager.Instance.fourKindComboScore;
        straightFlushComboScore = HandValueManager.Instance.straightFlushComboScore;
    }

    public void playHand()
    {
        if (getHandStrength(hand.hand) >= 200)
        {
            foreach (CardScript card in hand.hand)
            {
                card.toggleSelected();
            }
            StartCoroutine(DelayPlayHand());
        } 
        else
        {
            if (GetBestFlushSuit(hand.hand).Item2 >= 3) PlayForFlush();
            else if (FindBestStraightWindow(hand.hand).Item2 >= 3) PlayForStraight();
            else if (GetBestRank(hand.hand).Item2 >= 3) PlayForRanks(4);
            else PlayForRanks(2);
        }
    }

    private float getHandStrength(List<CardScript> cards)
    {
        float handStrength = 0;
        foreach (CardScript card in cards)
        {
            handStrength += card.value;
        }
        return Mathf.Floor(getHandTypeBase(cards) + handStrength / cards.Count);
    }

    private void PlayForRanks(int matchRank)
    {
        (int, int) bestRank = GetBestRank(hand.hand);
        if (bestRank.Item2 >= matchRank)
        {
            foreach (CardScript card in hand.hand)
            {
                card.toggleSelected();
            }
            StartCoroutine(DelayPlayHand());
        }
        else
        {
            foreach (CardScript card in hand.hand)
            {
                // Joker is always allowed
                if (card.value == 15)
                    continue;
                // If value exists in best window, consume one copy
                if (card.value != bestRank.Item1)
                {
                    card.toggleSelected(); // Mark for discard
                }
            }
            StartCoroutine(DelayPlayHand());
        }
    }

    private void PlayForFlush()
    {
        if (getHandTypeBase(hand.hand) == flushBase)
        {
            foreach (CardScript card in hand.hand)
            {
                card.toggleSelected();
            }
            StartCoroutine(DelayPlayHand());
        }
        else
        {
            string bestSuit = GetBestFlushSuit(hand.hand).Item1;

            foreach (CardScript card in hand.hand)
            {
                // Joker is always allowed
                if (card.value == 15)
                    continue;

                // If value exists in best window, consume one copy
                if (card.suit != bestSuit)
                {
                    card.toggleSelected(); // Mark for discard
                }
            }
            StartCoroutine(DelayPlayHand());
        }
    }

    public (int, int) GetBestRank(List<CardScript> cards)
    {
        Dictionary<int, int> rankCounts = new Dictionary<int, int>();
        int jokerCount = 0;
        // Count ranks
        foreach (var card in cards)
        {
            if (card.value == 15) jokerCount++; // Ignore jokers
            if (!rankCounts.ContainsKey((int)card.value))
                rankCounts[(int)card.value] = 0;
            rankCounts[(int)card.value]++;
        }
        // Find rank with highest count
        int bestRank = -1;
        int bestCount = 0;
        foreach (var kvp in rankCounts)
        {
            if (kvp.Value > bestCount)
            {
                bestCount = kvp.Value;
                bestRank = kvp.Key;
            }
        }
        return (bestRank, bestCount + jokerCount); // Returns null if no cards found
    }

    public (string, int) GetBestFlushSuit(List<CardScript> cards)
    {
        Dictionary<string, int> suitCounts = new Dictionary<string, int>();
        int jokerCount = 0;

        // Count suits
        foreach (var card in cards)
        {
            if (card.value == 15) jokerCount++; // Ignore jokers

            if (!suitCounts.ContainsKey(card.suit))
                suitCounts[card.suit] = 0;

            suitCounts[card.suit]++;
        }

        // Find suit with highest count
        string bestSuit = null;
        int bestCount = 0;

        foreach (var kvp in suitCounts)
        {
            if (kvp.Value > bestCount)
            {
                bestCount = kvp.Value;
                bestSuit = kvp.Key;
            }
        }

        return (bestSuit, bestCount + jokerCount); // Returns null if no cards found
    }


    private void PlayForStraight()
    {
        if (getHandTypeBase(hand.hand) == straightBase)
        {
            foreach (CardScript card in hand.hand)
            {
                card.toggleSelected(); // Select all if already a straight
            }
            StartCoroutine(DelayPlayHand());
        }
        else
        {
            int bestStart = FindBestStraightWindow(hand.hand).Item1;
            List<float> bestWindow = new List<float>();
            for (int i = 0; i < 5; i++)
            {
                bestWindow.Add(bestStart + i);
            }

            // Create a consumable copy of the best window values
            List<float> remainingValues = new List<float>(bestWindow);

            foreach (CardScript card in hand.hand)
            {
                // Joker is always allowed
                if (card.value == 15)
                    continue;

                // If value exists in best window, consume one copy
                if (remainingValues.Contains(card.value))
                {
                    remainingValues.Remove(card.value); // Only allow one copy
                }
                else
                {
                    card.toggleSelected(); // Mark for discard
                }
            }

            StartCoroutine(DelayPlayHand());
        }
    }

    private (int, int) FindBestStraightWindow(List<CardScript> cards)
    {
        // Extract ranks and count jokers (value == 0 treated as joker)
        List<float> ranks = new List<float>();
        int jokerCount = 0;

        foreach (var card in cards)
        {
            if (card.value == 15) jokerCount++;
            else ranks.Add(card.value);
        }

        HashSet<float> uniqueRanks = new HashSet<float>(ranks);
        int bestStart = -1;
        int minGaps = 6; // more than the max possible (5)

        for (int start = 2; start <= 10; start++)
        {
            int gaps = 0;
            for (int i = 0; i < 5; i++)
            {
                if (!uniqueRanks.Contains(start + i))
                    gaps++;
            }

            if (gaps <= jokerCount && gaps < minGaps)
            {
                minGaps = gaps - jokerCount;
                bestStart = start;
            }
        }

        return (bestStart, minGaps); // start of best window, and how many jokers it would use
    }

    IEnumerator DelayPlayHand()
    {
        yield return new WaitForSeconds(0.2f);

        PhotonView handView = hand.GetComponent<PhotonView>();
        handView.RPC("RPC_PlayHand", RpcTarget.AllBuffered);
    }

    private float getHandTypeBase(List<CardScript> cards)
    {
        List<int> ranks = new List<int>();
        List<string> suits = new List<string>();
        int blackJokerCount = 0;
        int redJokerCount = 0;

        foreach (CardScript card in cards)
        {
            if (card.value == 15)
            {
                if (card.suit == "red") redJokerCount++;
                else if (card.suit == "black") blackJokerCount++;
            }
            else
            {
                ranks.Add((int)card.value);
                suits.Add(card.suit);
            }
        }

        int totalJokers = blackJokerCount + redJokerCount;

        bool isFlush = CheckFlush(suits, totalJokers);
        bool isStraight = CheckStraight(ranks, totalJokers);

        // Build rank histogram and simulate joker assignment
        Dictionary<int, int> rankCounts = GetRankCountsWithJokers(ranks, totalJokers);
        int maxCount = rankCounts.Values.Max();

        // Determine hand strength
        if (isFlush && isStraight)
        {
            player.updateComboScore(straightFlushComboScore);
            return straightFlushBase;
        }
        if (maxCount == 4)
        {
            player.updateComboScore(fourKindComboScore);
            return fourKindBase;
        }
        if (maxCount == 3 && rankCounts.Count == 2)
        {
            player.updateComboScore(fullHouseComboScore);
            return fullHouseBase;
        }
        if (isFlush)
        {
            player.updateComboScore(flushComboScore);
            return flushBase;
        }
        if (isStraight)
        {
            player.updateComboScore(straightComboScore);
            return straightBase;
        }
        if (maxCount == 3)
        {
            player.updateComboScore(threeKindComboScore);
            return threeKindBase;
        }
        if (maxCount == 2 && rankCounts.Values.Count(v => v == 2) == 2)
        {
            player.updateComboScore(twoPairComboScore);
            return twoPairBase;
        }
        if (maxCount == 2)
        {
            player.updateComboScore(onePairComboScore);
            return onePairBase;
        }

        player.updateComboScore(highCardComboScore);
        return highCardBase;
    }

    // ----------- Helper Methods -----------

    private bool CheckFlush(List<string> suits, int jokerCount)
    {
        if (suits.Count == 0) return jokerCount >= 5;

        var suitCounts = suits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
        return suitCounts.Any(kvp => kvp.Value + jokerCount >= 5);
    }

    private bool CheckStraight(List<int> ranks, int jokerCount)
    {
        var uniqueRanks = ranks.Distinct().OrderBy(r => r).ToList();
        for (int start = 2; start <= 10; start++)
        {
            int gaps = 0;
            for (int i = 0; i < 5; i++)
            {
                if (!uniqueRanks.Contains(start + i)) gaps++;
            }
            if (gaps <= jokerCount) return true;
        }

        return false;
    }

    private Dictionary<int, int> GetRankCountsWithJokers(List<int> ranks, int jokerCount)
    {
        var rankCounts = new Dictionary<int, int>();
        foreach (var r in ranks)
        {
            if (!rankCounts.ContainsKey(r))
                rankCounts[r] = 1;
            else
                rankCounts[r]++;
        }

        // Greedily assign jokers to most frequent ranks to improve hand strength
        for (int i = 0; i < jokerCount; i++)
        {
            if (rankCounts.Count == 0)
            {
                rankCounts[2] = 1; // Assign arbitrary rank
                continue;
            }

            int target = rankCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            rankCounts[target]++;
        }

        return rankCounts;
    }

}
