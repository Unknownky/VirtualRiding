using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 80;
        Debug.Log($"current frame rate: {Application.targetFrameRate}");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
