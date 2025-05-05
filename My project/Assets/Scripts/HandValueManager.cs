using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
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

    public string getHandTypeString(List<CardScript> cards)
    {
        if (cards.Count == 0) return "";
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
            return "Straight\nFlush";
        } // Straight Flush
        if (maxCount == 4)
        {
            return "Quartet";
        } // Four of a Kind
        if (IsFullHouse(rankCounts))
        {
            return "Full\nHouse";
        } // Full House
        if (isFlush)
        {
            return "Flush";
        } // Flush
        if (isStraight)
        {
            return "Straight";
        } // Straight
        if (maxCount == 3)
        {
            return "Triplet";
        } // Three of a Kind
        if (maxCount == 2 && rankCounts.Count <= cards.Count - 2)
        {
            return "Two\nPair";
        } // Two Pair
        if (maxCount == 2)
        {
            return "Pair";
        } // One Pair
        return "High\nCard";// High Card
    }

    public float getHandTypeBase(List<CardScript> cards)
    { 
        if (cards.Count == 0) return 0;
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
            return straightFlushBase;
        }
        if (maxCount == 4)
        {
            return fourKindBase;
        }
        if (IsFullHouse(rankCounts))
        {
            return fullHouseBase;
        }
        if (isFlush)
        {
            return flushBase;
        }
        if (isStraight)
        {
            return straightBase;
        }
        if (maxCount == 3)
        {
            return threeKindBase;
        }
        if (maxCount == 2 && rankCounts.Values.Count(v => v == 2) == 2)
        {
            return twoPairBase;
        }
        if (maxCount == 2)
        {
            return onePairBase;
        }
        return highCardBase;
    }

    // ----------- Helper Methods -----------

    private bool IsFullHouse(Dictionary<int, int> rankCounts)
    {
        // Sort by count descending
        var counts = rankCounts.Values.OrderByDescending(c => c).ToList();

        // Must be at least 2 distinct ranks
        if (counts.Count < 2) return false;

        // Check for exactly one triplet and one pair using 5 total cards
        return counts[0] == 3 && counts[1] == 2 && counts.Take(2).Sum() == 5;
    }

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

        // Check for ace-low straight: A-2-3-4-5
        if (uniqueRanks.Contains(14)) // Ace
        {
            List<int> lowAceWindow = new List<int> { 1, 2, 3, 4, 5 };
            int gaps = 0;
            foreach (int val in lowAceWindow)
            {
                if (!uniqueRanks.Contains(val == 1 ? 14 : val)) // treat Ace as 1
                    gaps++;
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
