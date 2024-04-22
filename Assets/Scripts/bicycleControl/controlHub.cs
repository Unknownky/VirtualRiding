using UnityEngine;
using System.Collections;

// 这个脚本包含了所有传递给自行车脚本的变量。
// 因此，它只是所有控制变量的容器。
// 移动/键盘脚本将数字（浮点数、整数、布尔值）发送到这个脚本。

public class controlHub : MonoBehaviour  {//需要用于移动控制


    public float Vertical;//传递给自行车脚本的变量，用于加速/停止和倾斜自行车
    public float Horizontal;//传递给自行车脚本的变量，用于驾驶员的质量转移
    
    public float VerticalMassShift;//驾驶员质量沿自行车的平移变量
    public float HorizontalMassShift;//驾驶员质量横向自行车的平移变量

    public bool rearBrakeOn;//此变量告诉自行车脚本使用后刹车
    public bool restartBike;//此变量告诉自行车脚本重新启动
    public bool fullRestartBike; //此变量告诉自行车脚本进行完全重新启动

    public bool reverse;//用于反向速度

}