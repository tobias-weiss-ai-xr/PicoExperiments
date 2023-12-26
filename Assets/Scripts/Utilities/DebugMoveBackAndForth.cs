using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using UnityEngine;

public class DebugMoveBackAndForth : MonoBehaviour
{
    [Range(0.1f, 2f)]
    public float speed;
    public bool forward;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (forward)
        {
            this.transform.position += this.transform.forward * Mathf.Sin(Time.deltaTime) * speed;
        } else
        {
            this.transform.position -= this.transform.forward * Mathf.Sin(Time.deltaTime) * speed;
        }
        
    }
}
