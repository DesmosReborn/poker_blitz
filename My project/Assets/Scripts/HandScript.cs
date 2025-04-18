using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;

public class HandScript : MonoBehaviourPun
{
    public List<CardScript> hand;
    [SerializeField] private DeckScript deck;
    [SerializeField] GameObject attackPrefab;
    [SerializeField] GameObject cardSpawnVFX;
    [SerializeField] GameObject cardPlayVFX;
    [SerializeField] float drawCD = 0.3f;
    [SerializeField] AudioSource playHandAudio;
    [SerializeField] private Transform cardPlayPosition;
    [SerializeField] private TextMeshProUGUI handTypeUI;
    [SerializeField] private PhotonView playerView;

    private float[] handPositions = new float[] { -5, -2.5f, 0, 2.5f, 5 };

    // Start is called before the first frame update
    void Start()
    {
        if (!photonView.IsMine) return;
        photonView.RPC(nameof(RPC_InitializeHand), RpcTarget.AllBuffered);
        StartCoroutine(waitDraw(drawCD));
    }

    [PunRPC]
    private void RPC_InitializeHand()
    {
        hand = new List<CardScript>() { null, null, null, null, null };
        playerView = this.transform.parent.gameObject.GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [PunRPC]
    private void RPC_PlayHand()
    {
        List<int> playedCardViewIDs = new List<int>();
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].selected)
            {
                //hand[i].gameObject.layer = LayerMask.NameToLayer("No Post");
                hand[i].gameObject.GetComponent<SpriteRenderer>().sortingOrder = 7;
                float endRotationY = hand[i].transform.localEulerAngles.y + 360;
                Vector3 endPosition = hand[i].transform.localPosition;
                Vector3 endScale = new Vector3(0, hand[i].defaultScale.y, hand[i].defaultScale.z);
                GameObject vfx = Instantiate(cardPlayVFX);
                vfx.transform.position = hand[i].transform.position;
                hand[i].invokeCardAnim(endRotationY, endPosition, Vector3.zero, hand[i].playTime);
                playedCardViewIDs.Add(hand[i].GetComponent<PhotonView>().ViewID);
                hand[i] = null;
            }
        }
        if (playedCardViewIDs.Count > 0)
        {
            playHandAudio.Play();

            if (photonView.IsMine)
            {
                GameObject newAttack = PhotonNetwork.Instantiate(attackPrefab.name, cardPlayPosition.localPosition, cardPlayPosition.localRotation);
                PhotonView attackView = newAttack.GetComponent<PhotonView>();
                attackView.RPC("RPC_InitializeAttack", RpcTarget.All, playedCardViewIDs.ToArray(), playerView.ViewID);
            }

            if (photonView.IsMine)
            {
                StartCoroutine(waitDraw(drawCD));
            }
            updateHandType();
        }
    }

    IEnumerator waitDraw(float time)
    {
        yield return new WaitForSeconds(time);

        Draw();
    }

    private void Draw()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == null)
            {
                CardScript drawnCard = deck.DrawCard();
                if (drawnCard == null) return; // deck empty after recycling
                PhotonView cardView = drawnCard.GetComponent<PhotonView>();
                photonView.RPC(nameof(RPC_PlaceCardInHand), RpcTarget.AllBuffered, cardView.ViewID, i, photonView.ViewID);
            }
        }
    }

    [PunRPC]
    private void RPC_PlaceCardInHand(int cardViewID, int slotIndex, int handViewID)
    {
        PhotonView cardView = PhotonView.Find(cardViewID);
        if (cardView == null)
        {
            Debug.LogWarning("Card PhotonView not found: " + cardViewID);
            return;
        }

        CardScript card = cardView.GetComponent<CardScript>();
        if (card == null)
        {
            Debug.LogWarning("CardScript missing on view ID: " + cardViewID);
            return;
        }

        PhotonView handView = PhotonView.Find(handViewID);
        HandScript handObject = handView.GetComponent<HandScript>();
        handObject.hand[slotIndex] = card;
        card.transform.SetParent(this.transform, true);
        card.transform.localPosition = handPositions[slotIndex] * Vector3.right;
        card.transform.rotation = Quaternion.identity;
        card.gameObject.layer = LayerMask.NameToLayer("No Post");
        card.GetComponent<SpriteRenderer>().sortingOrder = 10;
        GameObject vfx = Instantiate(cardSpawnVFX);
        vfx.transform.position = card.transform.position;
        float endRotationY = card.transform.localEulerAngles.y + 720;
        card.invokeCardAnim(endRotationY, card.transform.localPosition, card.defaultScale, card.spawnTime);
    }

    public void updateHandType()
    {
        if (!photonView.IsMine) return;
        List<CardScript> selected = new List<CardScript>();
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] != null && hand[i].selected)
            {
                selected.Add(hand[i]);
            }
        }
        handTypeUI.text = getHandType(selected);
    }

    private string getHandType(List<CardScript> cards)
    {
        if (cards.Count == 0) return "";
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
            return "Straight\nFlush";
        } // Straight Flush
        if (maxCount == 4)
        {
            return "Quartet";
        } // Four of a Kind
        if (maxCount == 3 && rankCounts.Count == 2)
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
}
