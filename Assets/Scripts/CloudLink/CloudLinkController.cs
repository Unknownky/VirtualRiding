using System;
using AlibabaCloud;
using AmqpClient;
using UnityEngine;

public class CloudLinkController : MonoBehaviour
{
    // public float Vertical;//传递给自行车脚本的变量，用于加速/停止和倾斜自行车
    // public float Horizontal;//传递给自行车脚本的变量，用于驾驶员的质量转移

    // public float VerticalMassShift;//驾驶员质量沿自行车的平移变量
    // public float HorizontalMassShift;//驾驶员质量横向自行车的平移变量

    // public bool rearBrakeOn;//此变量告诉自行车脚本使用后刹车
    // public bool restartBike;//此变量告诉自行车脚本重新启动
    // public bool fullRestartBike; //此变量告诉自行车脚本进行完全重新启动

    // public bool reverse;//用于反向速度

    public static float Vertical;//传递给自行车脚本的变量，用于加速/停止和倾斜自行车
    public static float Horizontal;//传递给自行车脚本的变量，用于驾驶员的质量转移

    //TODO:完成摄像头的控制
    private GameObject ctrlHub;// 与对应自行车脚本的链接
    private controlHub outsideControls;// 与对应自行车脚本的链接


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("CloudLinkController Start");
        AmqpLinkTest.RunAmqpLink(null);
        ctrlHub = GameObject.Find("gameScenario");// 链接到名为"controlHub"的GameObject上的脚本
        outsideControls = ctrlHub?.GetComponent<controlHub>();// 与对应自行车脚本的链接
        Vertical = 0;
        Horizontal = 0;
    }

    //将控制更新与AMQP接收到的信息隔离开
   private void Update() {
        //设置移动控制
        outsideControls.Vertical = Vertical;
        outsideControls.Horizontal = Horizontal;
   }
    //由AMQP更新控制属性
   public static void SetAttribute(float vertical, float horizontal){
       Vertical = vertical;
       Horizontal = horizontal;
        //TODO:完成摄像头的控制
   }
}
