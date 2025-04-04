using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class DeckScript : MonoBehaviour
{
    public List<CardScript> deck;
    public List<CardScript> played;
    [SerializeField] GameObject cardPrefab;
    private int playerID;

    private string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
    private float[] numbers = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };  // 14 = Ace, 11 = Jack, 12 = Queen, 13 = King

    [SerializeField] bool addJokers;

    // Start is called before the first frame update
    void Start()
    {
        deck = new List<CardScript>();
        played = new List<CardScript>();
        playerID = this.transform.parent.GetComponent<PlayerScript>().view.ViewID;
        initializeDeck();
    }

    private void initializeDeck()
    {
        foreach (string suit in suits)
        {
            foreach (float number in numbers)
            {
                GameObject newCard = Instantiate(cardPrefab, this.transform);
                newCard.GetComponent<CardScript>().Initialize(number, suit, playerID);
                deck.Add(newCard.GetComponent<CardScript>());
            }
        }
        if (addJokers)
        {
            GameObject joker_of_red = Instantiate(cardPrefab, this.transform);
            joker_of_red.GetComponent<CardScript>().Initialize(15, "red", playerID);
            deck.Add(joker_of_red.GetComponent<CardScript>());
            GameObject joker_of_black = Instantiate(cardPrefab, this.transform);
            joker_of_black.GetComponent<CardScript>().Initialize(15, "black", playerID);
            deck.Add(joker_of_black.GetComponent<CardScript>());
        }
        shuffleDeck();
    }

    private void reInitializeDeck()
    {
        foreach (CardScript card in played)
        {
            card.transform.position = this.transform.position;
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

            // Swap deck[i] and deck[randomIndex]
            CardScript temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public CardScript draw()
    {
        if (deck.Count > 0)
        {
            CardScript card = deck[0];
            deck.Remove(card);
            return card;
        } else
        {
            reInitializeDeck();
            return draw();
        }
    }
}
