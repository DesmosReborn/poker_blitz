using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StyleMeterManager : MonoBehaviour
{
    [SerializeField] RawImage styleMeterRawImage;
    [SerializeField] RawImage styleMeterBGImage;
    [SerializeField] RectTransform styleMeterUI;

    private Color styleFColor = new Color(159 / 255f, 0, 93 / 255f);
    private Color styleEColor = new Color(146 / 255f, 39 / 255f, 143 / 255f);
    private Color styleDColor = new Color(133 / 255f, 96 / 255f, 168 / 255f);
    private Color styleCColor = new Color(65 / 255f, 97 / 255f, 202 / 255f);
    private Color styleBColor = new Color(242 / 255f, 101 / 255f, 33 / 255f);
    private Color styleAColor = new Color(234 / 255f, 22 / 255f, 22 / 255f);
    private Color styleSColor = new Color(255 / 255f, 242 / 255f, 0);

    private List<string> comboStrings = new List<string>() { "F", "E", "D", "C", "B", "A", "S", "SS", "SSS" };
    private List<float> styleMeterPulseFreqs = new List<float>() { 3f, 2f, 1.5f, 1f, 0.75f, 0.5f, 0.3f, 0.2f, 0.15f };
    private List<float> styleMeterPulseScales = new List<float>() { 1f, 1f, 1.05f, 1.05f, 1.1f, 1.1f, 1.15f, 1.15f, 1.15f };
    [SerializeField] private float styleMeterPulseFreq;
    [SerializeField] private float styleMeterPulseScale;
    [SerializeField] private float styleMeterPulseDuration;
    private Vector3 originalScale = Vector3.one;

    // Start is called before the first frame update
    void Start()
    {

        updateStyleMeter("F");
        updateStyleMeter("F");
        StartCoroutine(PulseRoutine(1));
        StartCoroutine(PulseRoutine(2));
    }

    public void updateStyleMeter(string rank)
    {
        string path = $"Art/UI/StyleMeter/styleMeter{rank}";
        Texture2D styleMeterTexture = Resources.Load<Texture2D>(path);
        int index = comboStrings.IndexOf(rank);

        styleMeterPulseFreq = styleMeterPulseFreqs[index];
        styleMeterPulseScale = styleMeterPulseScales[index];
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
            yield return StartCoroutine(PulseEffect());

            // Wait for styleMeterPulseFreq before pulsing again
            yield return new WaitForSeconds(styleMeterPulseFreq);
        }
    }

    IEnumerator PulseEffect()
    {
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
