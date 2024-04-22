using System.Data.Common;
using System.Transactions;
using UnityEngine;


/// <summary>
/// 控制自行车的脚本，其中需要接入进行实时更新的都注明了  **实时更新**
/// </summary>
public class BicycleController : MonoBehaviour
{
    [Header("运动参数")]
    public Rigidbody m_rigidbody;

    [Tooltip("当前移动方向")] public Vector3 currentMovingDirection { get { return transform.forward; } set { } }
    //暂时限定转动Y的Rotation值Vector(x,y,z)，方向向量需要标准化

    [Tooltip("移动速度使用的缩减比例")] public float decreaseScale = 100f;

    #region 内部参数
    private float angularVelocityMax;
    private float maxRidingSpeed;

    //运动脚本只需要使用currentAngularVelocity、accelerate两个参数即可
    private float currentAngularVelocity;//转动的角速度,负的向左转,正的向右转

    private float accelerate;//运动的加速度

    private float inverDecreaseScale;

    private float minRotation;

    private float maxRotation;

    private float currentYRotation => m_rigidbody.transform.rotation.y;
    #endregion


    private void Start()
    {
        ParameterInitAndWarning();
    }

    private void ParameterInitAndWarning()
    {
        if (m_rigidbody == null)
        {
#if UNITY_EDITOR
            Debug.Log("The Rigidbody is null");
#endif
            m_rigidbody = GetComponent<Rigidbody>();//获取刚体组件
        }
        currentMovingDirection = transform.forward;
        inverDecreaseScale = 1 / decreaseScale;
        DataServer.GetInstance().PlaceTheUnchangeableData(ref angularVelocityMax, ref maxRidingSpeed, ref minRotation, ref maxRotation);//不变参数的赋值
    }

    //接收数据
    private void Update()
    {
        //数据传入更新:    **TODO**   之后尝试对接数据
        MoveSimulateDataUpdate();

    }

    #region 模拟数据更新区域
    private void MoveSimulateDataUpdate()//数据不在当前脚本接收
    {
        //运动只需要获取accelerate、currentAngularVelocity两个参数
        DataServer.GetInstance().PlaceRealtimeDataForMove(ref accelerate, ref currentAngularVelocity);
    }
    #endregion




    private void FixedUpdate()
    {
        //物理更新
        PhysicsUpdate();
    }

    #region 物理更新区域
    private void PhysicsUpdate()
    {
        //根据传入的数据更新实际需要使用到的方向参数
        currentMovingDirectionUpdate();
        //更新速度
        VelocityUpdate();
        //动画更新
    }

    private void currentMovingDirectionUpdate()
    {
        float deltaAngular = currentAngularVelocity * Time.fixedDeltaTime;
#if UNITY_EDITOR
        Debug.Log(deltaAngular);
#endif
        // 计算目标旋转
        Quaternion targetRotation = Quaternion.Euler(0, deltaAngular, 0) * transform.rotation;
        // 平滑地插值到目标旋转
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * Mathf.Abs(currentAngularVelocity));
        ClampMoveingRotation();
    }

    private void ClampMoveingRotation()
    {
        if (!isValidRotation())
        {
            float fixedAngle = Mathf.Clamp(currentYRotation, minRotation, maxRotation);
            transform.rotation = Quaternion.Euler(0, fixedAngle, 0);
            //Mathf.Tan
        }
    }

    private bool isValidRotation()
    {
        if (currentYRotation >= minRotation && currentYRotation <= maxRotation)
            return true;
        return false;
    }
    private void VelocityUpdate()
    {
        if (m_rigidbody.velocity.sqrMagnitude > maxRidingSpeed * maxRidingSpeed)
        {
            return;
        }
        Vector3 velocityDelta = accelerate * currentMovingDirection * inverDecreaseScale;
        Vector3 newVelocity = new Vector3 (m_rigidbody.velocity.x+velocityDelta.x, 0, m_rigidbody.velocity.z+velocityDelta.z);
        newVelocity = newVelocity.magnitude * currentMovingDirection;
        m_rigidbody.velocity = new Vector3(newVelocity.x, m_rigidbody.velocity.y, newVelocity.z);
    }
    #endregion

}
