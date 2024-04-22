using UnityEngine;
using System.Collections;

public class bike_sound : MonoBehaviour
{

    public bicycle_code linkToBike;// 与对应自行车脚本建立链接

    private AudioSource skidSound;// 为刹车声音创建另一个音频源

    // 创建声音（在编辑器中将其链接到实际声音文件）
    public AudioClip skid;

    // 我们需要知道是否有车轮打滑
    public bool isSkidingFront = false;
    public bool isSkidingRear = false;

    private GameObject ctrlHub;// 带有脚本控制变量的游戏对象
    private controlHub outsideControls;// 与对应自行车脚本建立链接
    void Start()
    {
        ctrlHub = GameObject.Find("gameScenario");// 链接到带有脚本“controlHub”的游戏对象
        outsideControls = ctrlHub.GetComponent<controlHub>();// 将C#移动控制脚本连接到此脚本

        // 将声音分配给音频源
        skidSound = gameObject.AddComponent<AudioSource>();
        skidSound.loop = false;
        skidSound.playOnAwake = false;
        skidSound.clip = skid;
        skidSound.pitch = 1.0f;
        skidSound.volume = 1.0f;

        // 实时链接到当前自行车
        linkToBike = this.GetComponent<bicycle_code>();

    }
    void Update()
    {

        // 刹车声音
        if (linkToBike.coll_rearWheel.sidewaysFriction.stiffness < 0.5f && !isSkidingRear && linkToBike.bikeSpeed > 1)
        {
            skidSound.Play();
            isSkidingRear = true;
        }
        else if (linkToBike.coll_rearWheel.sidewaysFriction.stiffness >= 0.5f && isSkidingRear || linkToBike.bikeSpeed <= 1)
        {
            skidSound.Stop();
            isSkidingRear = false;
        }
        if (linkToBike.coll_frontWheel.brakeTorque >= (linkToBike.frontBrakePower - 10) && !isSkidingFront && linkToBike.bikeSpeed > 1)
        {
            skidSound.Play();
            isSkidingFront = true;
        }
        else if (linkToBike.coll_frontWheel.brakeTorque < linkToBike.frontBrakePower && isSkidingFront || linkToBike.bikeSpeed <= 1)
        {
            skidSound.Stop();
            isSkidingFront = false;
        }
    }
}