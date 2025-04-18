using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject Player1Prefab;
    [SerializeField] private GameObject Player2Prefab;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(InitializePlayer());
    }

    IEnumerator InitializePlayer()
    {
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(Player1Prefab.name, new Vector3(0, -7.5f, 0), Quaternion.identity);
        }
        else
        {
            PhotonNetwork.Instantiate(Player1Prefab.name, new Vector3(0, 7.5f, 0), Quaternion.Euler(0, 0, 180));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void gameOver()
    {
        Time.timeScale = 0.2f;
        Invoke("SwitchScene", 1f);
    }

    public void SwitchScene()
    {
        Time.timeScale = 1;
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("LoadingScene");
    }
}
