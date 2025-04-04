using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceAspectRatioScript : MonoBehaviour
{
    public float targetAspect = 16f / 9f; // 16:9 aspect ratio

    // Start is called before the first frame update
    void Start()
    {
        float screenAspect = (float)Screen.width / Screen.height;
        float scaleHeight = screenAspect / targetAspect;

        Camera mainCamera = GetComponent<Camera>();

        if (scaleHeight < 1.0f)
        {
            Rect rect = mainCamera.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            mainCamera.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = mainCamera.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            mainCamera.rect = rect;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
