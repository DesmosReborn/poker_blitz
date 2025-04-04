using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrientUIScript : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;


    // Start is called before the first frame update
    void Start()
    {
        mainCamera = FindObjectOfType<Camera>();
        orientUI();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void orientUI() 
    {
        if (mainCamera != null)
        {
            this.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward, -mainCamera.transform.up);
        }
        if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            this.transform.localScale = new Vector3(-1, -1, 1);
        }
    }
}
