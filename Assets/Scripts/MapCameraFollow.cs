using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 该脚本控制minimap的摄像头的跟随
/// </summary>
public class MapCameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothing;

    private void Start()
    {
        if (target == null)
            Debug.Log("followedTransform is null");
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 targetPos = new Vector3(target.position.x, transform.position.y, target.position.z);

        transform.position = Vector3.Lerp(transform.position, targetPos, smoothing);
    }
}
