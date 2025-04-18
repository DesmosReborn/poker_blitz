using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using Photon.Pun;

public class CardScript : MonoBehaviourPun
{
    public float value { get; set; }
    public string suit { get; set; }
    private SpriteRenderer sr;

    public bool selected;

    public Vector3 defaultScale = new Vector3(0.1f, 0.1f, 0.1f);
    private bool isAnimating = false;
    [SerializeField] float toggleTime = 0.2f;
    [SerializeField] public float playTime = 0.2f;
    [SerializeField] public float spawnTime = 0.2f;

    [SerializeField] float toggleUpDistance = 0.75f;
    [SerializeField] AudioSource selectedAudio;
    [SerializeField] AudioSource deselectedAudio;
    [SerializeField] public Collider2D col;

    private int playerID { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void toggleSelected()
    {
        if (!isAnimating)
        {
            selected = !selected;
            if (selected)
            {
                selectedAudio.Play();
                this.gameObject.layer = LayerMask.NameToLayer("Post");
            } else
            {
                deselectedAudio.Play();
                this.gameObject.layer = LayerMask.NameToLayer("No Post");
            }
            photonView.RPC(nameof(RPC_ToggleBloom), RpcTarget.OthersBuffered);
            Vector3 endPosition = selected
            ? transform.localPosition + new Vector3(0, toggleUpDistance, 0)
            : transform.localPosition - new Vector3(0, toggleUpDistance, 0);
            StartCoroutine(PlayCardAnim(transform.localEulerAngles.y + 360, endPosition, transform.localScale, toggleTime));
        }
    }

    private void setImage()
    {
        string imageName = $"{valToString()}_of_{suit}";
        string path = $"Art/Card Sprites/twi_sins_mentis/{imageName}";
        Sprite cardSprite = Resources.Load<Sprite>(path);

        if (cardSprite != null)
            sr.sprite = cardSprite;
        else
            Debug.LogError($"Card sprite {path} not found!");
    }

    private string valToString()
    {
        if (value == 11)
        {
            return "jack";
        } else if (value == 12) 
        {
            return "queen";
        } else if (value == 13)
        {
            return "king";
        } else if (value == 14)
        {
            return "ace";
        } else if (value == 15)
        {
            return "joker";
        }
        else
        {
            return value.ToString();
        }
    }

    public void invokeCardAnim(float endRotationY, Vector3 endPosition, Vector3 endScale, float time)
    {
        StartCoroutine(PlayCardAnim(endRotationY, endPosition, endScale, time));
    }

    IEnumerator PlayCardAnim(float endRotationY, Vector3 endPosition, Vector3 endScale, float time)
    {
        isAnimating = true;

        // Set target rotation and position
        float startRotationY = transform.localEulerAngles.y;
        Vector3 startPosition = transform.localPosition;
        Vector3 startScale = transform.localScale;

        float elapsedTime = 0f;

        while (elapsedTime < time)
        {
            float t = elapsedTime / time;

            // Smooth interpolation
            float currentRotationY = Mathf.Lerp(startRotationY, endRotationY, t);
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentRotationY, 0);
            transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are exactly set
        transform.localEulerAngles = new Vector3(0, 0, 0);
        transform.localPosition = endPosition;
        transform.localScale = endScale;

        isAnimating = false;
    }

    public string getName()
    {
        return $"{valToString()}_of_{suit}";
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!photonView.IsMine) return;
        if (collision.gameObject.tag == "card")
        {
            PhotonView enemyCardView = collision.gameObject.GetComponent<PhotonView>();
            if (enemyCardView == null)
            {
                Debug.LogWarning("Enemy Card PhotonView not found");
                return;
            }

            CardScript attackCard = enemyCardView.GetComponent<CardScript>();
            if (attackCard == null)
            {
                Debug.LogWarning("CardScript missing on view ID" + enemyCardView.ViewID);
                return;
            }

            if (!enemyCardView.IsMine)
            {
                PhotonView enemyAttackView = attackCard.transform.parent.gameObject.GetComponent<PhotonView>();
                PhotonView playerAttackView = this.transform.parent.gameObject.GetComponent<PhotonView>();
                AttackScript enemyAttack = enemyAttackView.GetComponent<AttackScript>();
                AttackScript playerAttack = playerAttackView.GetComponent<AttackScript>();
                if (enemyAttackView != null && playerAttackView != null && enemyAttack != null && playerAttack != null)
                {
                    CollisionManager.Instance.ResolveAttackCollision(playerAttack, enemyAttack);
                }
            }
        }

        if (collision.gameObject.tag == "Player")
        {
            PhotonView playerView = collision.transform.parent.gameObject.GetComponent<PhotonView>();
            PlayerScript player = playerView.GetComponent<PlayerScript>();
            PhotonView attackView = this.transform.parent.gameObject.GetComponent<PhotonView>();
            AttackScript attack = attackView.GetComponent<AttackScript>();
            if (photonView.IsMine != playerView.IsMine && player != null && attackView != null && attack != null)
            {
                CollisionManager.Instance.ResolvePlayerHit(player, attack);
            }
        }
    }

    [PunRPC]
    public void RPC_Initialize(float newValue, string newSuit, int newPlayerID)
    {
        playerID = newPlayerID;
        value = newValue;
        suit = newSuit;
        sr = GetComponent<SpriteRenderer>();
        setImage();
    }

    [PunRPC]
    public void RPC_ToggleBloom()
    {
        selected = !selected;
        if (selected)
        {
            this.gameObject.layer = LayerMask.NameToLayer("Post");
        }
        else
        {
            this.gameObject.layer = LayerMask.NameToLayer("No Post");
        }
    }
}
