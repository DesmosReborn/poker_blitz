using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class DamageableScript : MonoBehaviour
{
    [SerializeField] public float health = 100;
    protected Dictionary<string, int> reqs = new Dictionary<string, int>();
    [SerializeField] protected TextMeshProUGUI reqGui;
    protected SpriteRenderer sr;

    public virtual void Initialize(float newHealth, List<string> newReqs)
    {
        Start();
        health = newHealth;
        string reqGuiText = "";
        foreach (string req in newReqs)
        {
            reqGuiText += translateReqToText(req);
            if (reqs.ContainsKey(req))
            {
                reqs[req]++;
            }
            else
            {
                reqs[req] = 1;
            }
        }
        reqGui.text = reqGuiText;
    }

    public virtual string translateReqToText(string req)
    {
        if (req.Equals("Hearts"))
        {
            return "♥";
        }
        else if (req.Equals("Diamonds"))
        {
            return "♦";
        }
        else if (req.Equals("Spades"))
        {
            return "♠";
        }
        else if (req.Equals("Clubs"))
        {
            return "♣";
        }
        else if (req.Equals("11"))
        {
            return "J";
        }
        else if (req.Equals("12"))
        {
            return "Q";
        }
        else if (req.Equals("13"))
        {
            return "K";
        }
        else if (req.Equals("14"))
        {
            return "A";
        }
        else
        {
            return req;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected virtual void damageCheck(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("playerAttack"))
        {
            AttackScript attack = collision.gameObject.GetComponent<AttackScript>();
            Dictionary<string, int> attackAttr = new Dictionary<string, int>();
            int redJokerCount = 0;
            int blackJokerCount = 0;
            foreach (CardScript card in attack.cards)
            {
                if (card.value == 15)
                {
                    if (card.suit == "red") redJokerCount++;
                    if (card.suit == "black") blackJokerCount++;
                }
                else
                {
                    if (attackAttr.ContainsKey(card.suit))
                    {
                        attackAttr[card.suit]++;
                    }
                    else
                    {
                        attackAttr[card.suit] = 1;
                    }

                    if (attackAttr.ContainsKey(card.value.ToString()))
                    {
                        attackAttr[card.value.ToString()]++;
                    }
                    else
                    {
                        attackAttr[card.value.ToString()] = 1;
                    }
                }
            }
            int suitGaps = 0;
            int rankGaps = 0;
            foreach (string key in reqs.Keys)
            {
                if (float.TryParse(key, out _)) {
                    if (!attackAttr.ContainsKey(key))
                    {
                        rankGaps += reqs[key];
                    }
                    else
                    {
                        if (attackAttr[key] < reqs[key])
                        {
                            rankGaps += reqs[key] - attackAttr[key];
                        }
                    }
                } else {
                    if (!attackAttr.ContainsKey(key))
                    {
                        suitGaps += reqs[key];
                    }
                    else
                    {
                        if (attackAttr[key] < reqs[key])
                        {
                            suitGaps += reqs[key] - attackAttr[key];
                        }
                    }
                }
            }
            bool satisfied = ((rankGaps <= (redJokerCount + blackJokerCount)) && (suitGaps <= (redJokerCount + blackJokerCount)));
            if (satisfied)
            {
                takeDamage(collision.GetComponent<AttackScript>().currPower);
            }
        }
    }

    public virtual void takeDamage(float damage)
    {
        health -= damage;
        StartCoroutine(FlashRed(0.2f));
        if (health <= 0)
        {
            die();
        }
    }

    protected virtual void die()
    {
        Destroy(gameObject);
    }

    protected virtual IEnumerator FlashRed(float time)
    {
        sr.color = Color.red;

        yield return new WaitForSeconds(time);

        sr.color = new Color(1, 1, 1);
    }
}
