using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public TMP_InputField createInput;
    public TMP_InputField joinInput;
    //[SerializeField] private GameObject waitingText;

    public void createRoom()
    {
        PhotonNetwork.CreateRoom(createInput.text);
    }

    public void joinRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(joinInput.text);
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join failed: {message}");
    }
}
