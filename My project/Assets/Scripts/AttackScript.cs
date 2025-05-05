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
    private bool attackedThisFrame = false;

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

    [SerializeField] float baseMoveSpeed;
    [SerializeField] float initialMoveSpeed;
    [SerializeField] float decayRate;
    private float moveSpeed;

    [SerializeField] GameObject cardSpawnVFX;
    [SerializeField] TextMeshProUGUI dmgUI;

    [SerializeField] private PhotonView view;
    public bool isAIControlled = false;

    [PunRPC]
    private void RPC_InitializeAttack(int[] cardViewIDs, int playerViewID, bool newIsAIControlled=false)
    {
        Start();
        isAIControlled = newIsAIControlled;
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
        if (!playerView.IsMine || isAIControlled)
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

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine && !isAIControlled) return; // only owner moves/updates attack

        moveSpeed = Mathf.Clamp(moveSpeed - decayRate * Time.deltaTime, baseMoveSpeed, initialMoveSpeed);
        attackedThisFrame = false;
        move();
    }

    private float calculateHandStrength()
    {
        float handStrength = 0;
        foreach (CardScript card in cards)
        {
            handStrength += card.value;
        }
        return Mathf.Floor(getHandTypeBase() + handStrength / cards.Count);
    }

    private float getHandTypeBase()
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
        if (IsFullHouse(rankCounts))
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


    private void move()
    {
        this.transform.position += this.transform.up * moveSpeed * Time.deltaTime;
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (!photonView.IsMine && !isAIControlled) return;
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
