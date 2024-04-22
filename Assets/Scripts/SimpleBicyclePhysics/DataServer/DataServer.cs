using UnityEngine;

/// <summary>
/// 接受数据的脚本，并且提供更新的数据的接口，暂时不考虑耦合的问题
/// </summary>
public class DataServer : MonoBehaviour//挂载在GameManager上进行数据获取与脚本控制的分离，使用单例模式
{
    private static DataServer instance;//单例保证只有一个物体挂载了

    public CommonPlayDataAccess commonPlayDataAccess;

    [Header("需要设置的参数")]
    public int frameRate = 60;//帧率

    //
    /// <summary>
    /// 车子的加速度直接作用到方向上的速度，需要实时更新
    /// </summary>
    [Range(0f, 4f), Tooltip("骑自行车的加速度")] public float defaultAccelerate = 2f;

    [Tooltip("最大的角速度")] public float angularVelocityMax = 10f;//默认设置为10f，一般不用更新

    [Tooltip("最大骑行速度")] public float maxRidingSpeed = 10f;//默认设置为10f，一般不用更新

    [Tooltip("键盘操作的车头转动速度")] public float turningHeadSpeed = 20f;//用于键盘转动车头的控制

    [Tooltip("运动角度范围"), SerializeField, Range(-180f, 0f)] private float minRotation;

    [Tooltip("运动角度范围"), SerializeField, Range(0f, 180f)] private float maxRotation;


    [Header("自行车实时运动参数")]

    //
    /// <summary>
    /// 车头当前转动的角度，需要实时更新，之后根据相关物理进行角度的纠正
    /// </summary>
    [Range(-90f, 90f), Tooltip("当前自行车车头的角度")] public float angle;//暂时采用角度制

    //
    [Tooltip("当前的角速度")] public float currentAngularVelocity; //需要实时更新

    //
    [HideInInspector]public float angularVelocityPercent;

    //
    [HideInInspector]public float horizontal;//暂时用于测试展示

    [HideInInspector] public float vertical;

    [HideInInspector]public float accelerate;

    private void Awake()
    {
        //同时是跨区域保存的，在Persistance场景中
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = frameRate;//设置运行的帧率
    }

    #region 单例模式暴露函数
    public static DataServer GetInstance()
    {
        return instance;
    }
    #endregion


    #region 接口区域
    public void PlaceTheUnchangeableData(ref float angularVelocityMax, ref float maxRidingSpeed, ref float minRotation, ref float maxRotation)
    {
        angularVelocityMax = instance.angularVelocityMax;
        maxRidingSpeed = instance.maxRidingSpeed;
        minRotation = instance.minRotation;
        maxRotation = instance.maxRotation;
    }

    public void PlaceRealtimeDataForMove(ref float accelerate, ref float currentAngularVelocity)
    {
        //只需要给运动提供accelerate和currentAngularVelocity两个参数
        accelerate = instance.accelerate;
        currentAngularVelocity = instance.currentAngularVelocity;
    }
    #endregion

    private void Update()
    {
        SimulateDataUpdate();
    }

    #region 数据更新区域
    /// <summary>
    /// 模拟参数的更新，使用多态进行不同更新方式的匹配，只需要更改放入的类即可
    /// </summary>
    private void SimulateDataUpdate()
    {
        //TODO：设备检测，来使用多态
        IDataAccess dataAccess = instance.commonPlayDataAccess;
        dataAccess.SimulateDataUpdate();
    }
    #endregion
}
