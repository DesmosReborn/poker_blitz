using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;

public class ConnectToServerScript : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        StartCoroutine(ResetAndConnect());
    }

    IEnumerator ResetAndConnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();

            while (PhotonNetwork.IsConnected || PhotonNetwork.NetworkClientState != ClientState.Disconnected)
            {
                yield return null;
            }
        }

        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.LoadLevel("LobbyScene");
    }
}
