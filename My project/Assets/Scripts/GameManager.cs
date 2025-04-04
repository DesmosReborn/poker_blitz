using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
    [SerializeField] RawImage styleMeterRawImageP1;
    [SerializeField] RawImage styleMeterBGImageP1;
    [SerializeField] RectTransform styleMeterUIP1;

    [SerializeField] RawImage styleMeterRawImageP2;
    [SerializeField] RawImage styleMeterBGImageP2;
    [SerializeField] RectTransform styleMeterUIP2;

    [SerializeField] PlayerScript player1;
    [SerializeField] PlayerScript player2;

    private Color styleFColor = new Color(159 / 255f, 0, 93 / 255f);
    private Color styleEColor = new Color(146 / 255f, 39 / 255f, 143 / 255f);
    private Color styleDColor = new Color(133 / 255f, 96 / 255f, 168 / 255f);
    private Color styleCColor = new Color(65 / 255f, 97 / 255f, 202 / 255f);
    private Color styleBColor = new Color(242 / 255f, 101 / 255f, 33 / 255f);
    private Color styleAColor = new Color(234 / 255f, 22 / 255f, 22 / 255f);
    private Color styleSColor = new Color(255 / 255f, 242 / 255f, 0);

    private List<string> comboStrings = new List<string>() { "F", "E", "D", "C", "B", "A", "S", "SS", "SSS" };
    private List<float> styleMeterPulseFreqs = new List<float>() { 3f, 2f, 1.5f, 1f, 0.75f, 0.5f, 0.3f, 0.2f, 0.15f };
    private List<float> styleMeterPulseScales = new List<float>() { 1f, 1f, 1.05f, 1.05f, 1.1f, 1.1f, 1.15f, 1.15f, 1.15f};
    [SerializeField] private float styleMeterPulseFreqP1;
    [SerializeField] private float styleMeterPulseScaleP1;
    [SerializeField] private float styleMeterPulseDurationP1;

    [SerializeField] private float styleMeterPulseFreqP2;
    [SerializeField] private float styleMeterPulseScaleP2;
    [SerializeField] private float styleMeterPulseDurationP2;
    private Vector3 originalScale = Vector3.one;

    // Start is called before the first frame update
    void Start()
    {
        updateStyleMeter("F", 1);
        updateStyleMeter("F", 2);
        StartCoroutine(PulseRoutine(1));
        StartCoroutine(PulseRoutine(2));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("MenuScene");
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
        SceneManager.LoadScene("LobbyScene");
    }

    public void updateStyleMeter(string rank, int player)
    {
        RawImage styleMeterRawImage = player == 1 ? styleMeterRawImageP1 : styleMeterRawImageP2;
        RawImage styleMeterBGImage = player == 1 ? styleMeterBGImageP1 : styleMeterBGImageP2;
        RectTransform styleMeterUI = player == 1 ? styleMeterUIP1 : styleMeterUIP2;

        string path = $"Art/UI/StyleMeter/styleMeter{rank}";
        Texture2D styleMeterTexture = Resources.Load<Texture2D>(path);
        int index = comboStrings.IndexOf(rank);

        if (player == 1)
        {
            styleMeterPulseFreqP1 = styleMeterPulseFreqs[index];
            styleMeterPulseScaleP1 = styleMeterPulseScales[index];
        }
        else
        {
            styleMeterPulseFreqP2 = styleMeterPulseFreqs[index];
            styleMeterPulseScaleP2 = styleMeterPulseScales[index];
        }

        if (styleMeterTexture != null)
        {
            styleMeterRawImage.texture = styleMeterTexture;

            if (rank.Equals("F"))
            {
                styleMeterBGImage.color = styleFColor;
            }
            else if (rank.Equals("E"))
            {
                styleMeterBGImage.color = styleEColor;
            }
            else if (rank.Equals("D"))
            {
                styleMeterBGImage.color = styleDColor;
            }
            else if (rank.Equals("C"))
            {
                styleMeterBGImage.color = styleCColor;
            }
            else if (rank.Equals("B"))
            {
                styleMeterBGImage.color = styleBColor;
            }
            else if (rank.Equals("A"))
            {
                styleMeterBGImage.color = styleAColor;
            }
            else
            {
                styleMeterBGImage.color = styleSColor;
            }
        }
        else
            Debug.LogError($"{path} not found!");
    }

    IEnumerator PulseRoutine(int player)
    {
        while (true)
        {
            // Expand and shrink over styleMeterPulseDuration
            yield return StartCoroutine(PulseEffect(player));

            float styleMeterPulseFreq = player == 1 ? styleMeterPulseFreqP1 : styleMeterPulseFreqP2;

            // Wait for styleMeterPulseFreq before pulsing again
            yield return new WaitForSeconds(styleMeterPulseFreq);
        }
    }

    IEnumerator PulseEffect(int player)
    {
        RectTransform styleMeterUI = player == 1 ? styleMeterUIP1 : styleMeterUIP2;
        float styleMeterPulseDuration = player == 1 ? styleMeterPulseDurationP1 : styleMeterPulseDurationP2;
        float styleMeterPulseScale = player == 1 ? styleMeterPulseScaleP1 : styleMeterPulseScaleP2;
        float elapsedTime = 0f;

        // Expand phase
        while (elapsedTime < styleMeterPulseDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (styleMeterPulseDuration / 2f); // Normalize to [0,1]
            float scale = Mathf.Lerp(1f, styleMeterPulseScale, t);
            styleMeterUI.localScale = originalScale * scale;
            yield return null;
        }

        elapsedTime = 0f;

        // Shrink phase
        while (elapsedTime < styleMeterPulseDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (styleMeterPulseDuration / 2f);
            float scale = Mathf.Lerp(styleMeterPulseScale, 1f, t);
            styleMeterUI.localScale = originalScale * scale;
            yield return null;
        }

        styleMeterUI.localScale = originalScale; // Ensure reset to original
    }
}
