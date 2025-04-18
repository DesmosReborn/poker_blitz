using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using Photon.Pun;

public class DeckScript : MonoBehaviourPun
{
    public List<CardScript> deck;
    public List<CardScript> played;
    [SerializeField] GameObject hand;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] private PhotonView view;
    private int playerID;

    private string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
    private float[] numbers = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };  // 14 = Ace, 11 = Jack, 12 = Queen, 13 = King

    [SerializeField] bool addJokers;

    // Start is called before the first frame update
    void Start()
    {
        if (!view.IsMine) return;
        deck = new List<CardScript>();
        played = new List<CardScript>();
        playerID = this.transform.parent.GetComponent<PlayerScript>().view.ViewID;
        view = GetComponent<PhotonView>();

        initializeDeck();
    }

    private void initializeDeck()
    {
        if (!view.IsMine) return;
        foreach (string suit in suits)
        {
            foreach (float number in numbers)
            {
                GameObject newCard = PhotonNetwork.Instantiate(cardPrefab.name, this.transform.localPosition, Quaternion.identity);
                PhotonView cardView = newCard.GetComponent<PhotonView>();
                cardView.RPC("RPC_Initialize", Photon.Pun.RpcTarget.AllBuffered, number, suit, playerID);
                photonView.RPC("SetParentRPC", RpcTarget.AllBuffered, cardView.ViewID, photonView.ViewID);
            }
        }
        if (addJokers)
        {
            var jokers = new[] { "red", "black" };
            foreach (string suit in jokers)
            {
                GameObject joker = PhotonNetwork.Instantiate(cardPrefab.name, this.transform.localPosition, Quaternion.identity);
                PhotonView jokerView = joker.GetComponent<PhotonView>();
                jokerView.RPC("RPC_Initialize", Photon.Pun.RpcTarget.AllBuffered, 15f, suit, playerID);
                photonView.RPC("SetParentRPC", RpcTarget.AllBuffered, jokerView.ViewID, photonView.ViewID);
            }
        }
        shuffleDeck();
    }

    [PunRPC]
    private void RPC_ReInitializeDeck()
    {
        foreach (CardScript card in played)
        {
            card.transform.position = this.transform.localPosition;
            deck.Add(card);
        }
        played.Clear();
        shuffleDeck();
    }

    public void shuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public CardScript DrawCard()
    {
        if (deck.Count == 0)
        {
            photonView.RPC(nameof(RPC_ReInitializeDeck), RpcTarget.All);
        }

        // Get the card to draw
        CardScript card = deck[0];
        PhotonView cardView = card.GetComponent<PhotonView>();
        photonView.RPC(nameof(RPC_RemoveCardFromDeck), RpcTarget.All, cardView.ViewID);

        // Send card info to all clients
        return card;
    }

    [PunRPC]
    private void RPC_RemoveCardFromDeck(int cardViewID)
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

        deck.Remove(card);
    }


    [PunRPC]
    void SetParentRPC(int childID, int parentID)
    {
        PhotonView childPV = PhotonView.Find(childID);
        PhotonView parentPV = PhotonView.Find(parentID);

        if (childPV != null && parentPV != null)
        {
            childPV.transform.SetParent(parentPV.transform, false);

            // Add card to deck list if this deck owns the parent view
            if (this.photonView.ViewID == parentID)
            {
                deck.Add(childPV.GetComponent<CardScript>());
            }
        }
    }
}
