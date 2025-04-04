using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXScript : MonoBehaviour
{
    [SerializeField] float life = 1;
    [SerializeField] bool randomizeRotation = false;

    // Start is called before the first frame update
    void Start()
    {
        if (randomizeRotation)
        {
            this.transform.Rotate(0, 0, Random.Range(0, 360));
        }
    }

    // Update is called once per frame
    void Update()
    {
        life -= Time.deltaTime;
        if (life < 0)
        {
            Destroy(gameObject);
        }
    }
}
