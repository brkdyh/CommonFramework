using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleEx : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DebugConsole.StartDebugConsole(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Test Log");
            Debug.LogWarning("Test Warning");
            Debug.LogError("Test Log Error");
        }
    }
}
