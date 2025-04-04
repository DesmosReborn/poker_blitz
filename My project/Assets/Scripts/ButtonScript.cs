using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playGame()
    {
        Debug.Log("Loading Scene");
        SceneManager.LoadScene("GameScene");
    }

    public void playGameEasy()
    {
        Debug.Log("Loading Scene");
        SceneManager.LoadScene("EasyScene");
    }

    public void playGameNormal()
    {
        Debug.Log("Loading Scene");
        SceneManager.LoadScene("NormalScene");
    }

    public void playGameHard()
    {
        Debug.Log("Loading Scene");
        SceneManager.LoadScene("HardScene");
    }

    public void closeGame()
    {
        Application.Quit();
    }
}
