using UnityEditor;
using UnityEngine;

/// <summary>
/// 该脚本使用自行车的运动学模型进行车辆的控制
/// </summary>
public class ModelMoveController : MonoBehaviour
{
    //其中的角度较多涉及到三角函数的计算，统一使用弧度制，旋转操作使用四元数

    /// <summary>
    /// 自行车的刚体
    /// </summary>
    [SerializeField, Tooltip("自行车的刚体")] private Rigidbody m_rigidbody;
    /// <summary>
    /// 后轮转角，在自行车模型中为0rad
    /// </summary>
    [SerializeField, Range(0f,0f), Tooltip("自行车运动模型不使用后轮转角")]private float rearWheelSteeringAngle = 0f;
    /// <summary>
    /// 前轴距，自行车质心到前轮的距离
    /// </summary>
    [SerializeField, Range(0.5f, 1f), Tooltip("前轴距(推荐使用0.5m)")]private float frontWheelBase = 0.5f;
    /// <summary>
    /// 后轴距，自行车质心到后轮的距离
    /// </summary>
    [SerializeField, Range(0.5f, 1f), Tooltip("后轴距(推荐使用0.5m)")] private float rearWheelBase = 0.5f;
    /// <summary>
    /// 轴距，前后轴距之和
    /// </summary>
    private float wheelBase;
    /// <summary>
    /// 滑移角，车辆速度与纵轴的夹角,含正负号(规定左边为负，右边为正)
    /// </summary>
    private float slipAngle => Mathf.Deg2Rad*Vector3.SignedAngle(m_rigidbody.transform.forward, m_rigidbody.velocity, m_rigidbody.transform.up);
    /// <summary>
    /// 当前速度的模长
    /// </summary>
    private float velocityMagnitude => m_rigidbody.velocity.magnitude;
    /// <summary>
    /// 航向角，即为车身纵轴与X轴的夹角
    /// </summary>
    private float headingAngle;
    /// <summary>
    /// 自行车的加速度
    /// </summary>
    [SerializeField, Range(0.5f, 20f), Tooltip("加速度")] private float acceleration = 0.5f;

    [Header("键盘控制时运行时参数")]
    /// <summary>
    /// 用来接收输入的轴向值
    /// </summary>
    [SerializeField, Range(-1f,1f), Tooltip("水平轴向值")]private float horizontal;

    [SerializeField, Range(-1f, 1f), Tooltip("前后轴向值")] private float vertical;

    [SerializeField, Range(0.5f, 10f), Tooltip("自行车车头转到头用时")] private float turnMaxAngleTimeCost;

    [Header("自行车控制时运行参数")]
    /// <summary>
    /// 前轮转角，车头转向角度，使用弧度制
    /// </summary>
    [SerializeField, Range(-Mathf.PI,Mathf.PI)]private float frontWheelSteeringAngle;

    public enum ControlMode//控制模式
    {
        KeyBoardControl,
        BicycleControl,
    }

    private ControlMode controlMode = ControlMode.KeyBoardControl;

    private void Start()
    {
        
    }

    private void Update()
    {
        if(controlMode == ControlMode.KeyBoardControl)
        {
            vertical = Input.GetAxisRaw("Vertical");
            horizontal = Input.GetAxisRaw("Horizontal");//通过键盘获取输入
        }
        else
        {
            //获取自行车数据

        }
    }

    //进行物理更新
    private void FixedUpdate()
    {
        

    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(ModelMoveController))]
public class ModelMoveControllerCustomInspectorEditor: Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();//显示原本的Inspector
        EditorGUILayout.LabelField("自行车运动学模型并不使用后轮转角，但是保留接口便于之后的更改");
    }
}
#endif
