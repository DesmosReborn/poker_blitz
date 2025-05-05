using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public static class GameMode
{
    public static bool IsSinglePlayer = false;
}

public class RoomManager : MonoBehaviourPunCallbacks
{
    public TMP_InputField createInput;
    public TMP_InputField joinInput;
    //[SerializeField] private GameObject waitingText;

    public void createRoom()
    {
        GameMode.IsSinglePlayer = false;
        PhotonNetwork.OfflineMode = false;
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;           // ✅ Set this to how many players you want
        options.IsVisible = true;
        options.IsOpen = true;

        PhotonNetwork.CreateRoom(createInput.text, options);
    }

    public void joinRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            GameMode.IsSinglePlayer = false;
            PhotonNetwork.OfflineMode = false;
            PhotonNetwork.JoinRoom(joinInput.text);
        }
    }

    IEnumerator SwitchToOfflineModeAndStart()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();

            // Wait until disconnected
            while (PhotonNetwork.IsConnected || PhotonNetwork.NetworkClientState != ClientState.Disconnected)
            {
                yield return null;
            }
        }

        // Now it's safe to go offline
        GameMode.IsSinglePlayer = true; // Set the game mode to single player
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.CreateRoom("SinglePlayerRoom"); // or JoinOrCreateRoom
    }


    public void joinSingePlayer()
    {
        StartCoroutine(SwitchToOfflineModeAndStart());
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (GameMode.IsSinglePlayer)
            {
                SceneManager.LoadScene("SinglePlayerScene");
            }
            else
            {
                SceneManager.LoadScene("GameScene");
            }
        }
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join failed: {message}");
    }
}
