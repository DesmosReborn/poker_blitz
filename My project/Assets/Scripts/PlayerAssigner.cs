using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerAssigner : MonoBehaviourPunCallbacks
{
    public GameObject player1Object;
    public GameObject player2Object;

    void Start()
    {
        AssignPlayerToCharacter();
    }

    void AssignPlayerToCharacter()
    {
        // Determine your role based on join order
        Player[] sortedPlayers = PhotonNetwork.PlayerList; // Sorted by join order (ascending)

        if (PhotonNetwork.LocalPlayer == sortedPlayers[0])
        {
            photonView.RPC("AssignControl", RpcTarget.AllBuffered, player1Object.name, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else if (PhotonNetwork.PlayerList.Length > 1 && PhotonNetwork.LocalPlayer == sortedPlayers[1])
        {
            photonView.RPC("AssignControl", RpcTarget.AllBuffered, player2Object.name, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    void AssignControl(string playerObjectName, int actorNumber)
    {
        GameObject playerObj = GameObject.Find(playerObjectName);
        PhotonView view = playerObj.GetComponent<PhotonView>();

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            view.TransferOwnership(actorNumber);

            // Enable control locally
            playerObj.GetComponent<PlayerScript>().enabled = true;
        }
        else
        {
            // Disable control for non-owners
            playerObj.GetComponent<PlayerScript>().enabled = false;
        }
    }
}
