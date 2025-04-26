using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManagerScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void joinSinglePlayer()
    {
        SceneManager.LoadScene("SinglePlayerScene");
    }

    public void joinMultiPlayer()
    {
        SceneManager.LoadScene("LoadingScene");
    }
}
