using System;
using System.Transactions;
using UnityEditor;
using UnityEngine;

namespace DropInScripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class MoveControllerDropIn : MonoBehaviour
    {
        #region 暴露在Inspector的参数
        [Header("角色移动")]
        public Rigidbody rb;

        [Range(1, 10), Tooltip("m2/s")][SerializeField] private float moveacce = 3;//v = at;
        [Range(1, 10), Tooltip("m2/s")][SerializeField] private float stopacce = 1;

        [Tooltip("角色开始运行时的速度")][SerializeField] private float initialInstantSpeed = 0.2f;

        [Header("角色运动曲线参数")]
        [Tooltip("使用曲线生成运动")] public bool UsingCurveMove = false;
        [Tooltip("速度比例，避免浮点运算过多"), Range(1, 100)] public float SpeedScale = 100f;
        [Tooltip("内部使用直线进行简单模拟")] public readonly bool UsingLineModel = true;
        [Tooltip("加速用时s")][SerializeField] private float accFullSpeedTime = 1.5f;
        [Tooltip("减速用时s(可选默认1/3 accTime)")][SerializeField] private float decFullSpeedTime = 0f;
        [Tooltip("m/s")][SerializeField] private float maxSpeed = 4;
        /// <summary>
        /// 根据FixedUpdate中的CurveMove的运算来保存当前的速度的数值
        /// </summary>
        public float currentCachedSpeed => cachedRbVelocity.magnitude;

        [Tooltip("最小的速度")][SerializeField]private float minSpeed = 0;

        [Serializable]
        public struct ThreeFrameOffset
        {
            public int start_frameOffset;
            public int max_frameOffset;
            public int end_frameOffset;
        }

        [Serializable]
        public struct ThreeFrameSmoothWeight
        {
            public float start_strength;
            public float maxtime_strength;
            public float end_strength;
        }
        //三帧数值
        [Tooltip("三帧各帧偏移的数值(默认全0)")][SerializeField]private ThreeFrameOffset threeFrame;

        //三帧平滑强度
        [Tooltip("三帧平滑强度(默认全0)")][SerializeField]private ThreeFrameSmoothWeight treeFrameSmoothWeight;

        [Header("角色运动曲线")]
        [HideInInspector] public AnimationCurve speedCurve;//规定需要正常形状的钟型曲线,可以提供部分预设，直接进行运动曲线的生成

        #endregion
        /// <summary>
        /// 当前通用控制脚本模拟运行的方式
        /// </summary>
        private SimulateMode simulateMode;//私有的，隐藏，通过方法进行更改

        //枚举类型本身就是静态的
        public enum SimulateMode
        {
            EightDirectionMode,
            TwoDirectionMode,
        }

        #region 运动方向参数
        [Range(0, 1)] private float horizontal = 0;
        [Range(0, 1)] private float vertical = 0;
        /// <summary>
        /// 运动方向参数，暴露用于对接运动相关的动画器，且始终是单位向量
        /// </summary>
        public Vector2 direction;
        [Range(20f,200f),Tooltip("速度向量平方和截断")][SerializeField] private float velocityPowThreshold = 200f;
        [Range(1f, 2f), Tooltip("速度截断")][SerializeField] private float velocityThreshold = 1.5f;
        //默认的加速与减速时间比例
        private float defaultAccTimeRatioDecTime = 0.33f;
        /// <summary>
        /// 反转速度比例
        /// </summary>
        private float InverseSpeedScale;
        /// <summary>
        /// 缓存与rb.Velocity平行的应用了SpeedScale的一个向量，给rb.Velocity做了一层隔离层
        /// </summary>
        public Vector2 cachedRbVelocity;
        private float cachedMaxSpeed;//同理进行缓存，建立界面和内部数据的隔离层
        private float cachedMinSpeed;
        private float cachedInitialInstantSpeed;
        #endregion


        #region 曲线迭代参数
        public float endTime
        {
            get
            {
                //处理decFullSpeedTime为默认的情况
                if (decFullSpeedTime == 0)
                    decFullSpeedTime = accFullSpeedTime * defaultAccTimeRatioDecTime;
                return accFullSpeedTime + decFullSpeedTime;
            }
            set { }
        } //曲线最后的时刻，减少运算

        private float velocityThresholdScaledQuar;

        #region 线性模拟参数
        /// <summary>
        /// 加速斜率，正数
        /// </summary>
        private float accLinearSlope;
        /// <summary>
        /// 减速斜率，负数
        /// </summary>
        private float decLinearSlope;
        /// <summary>
        /// 对应accSlope的每帧增加向量模长
        /// </summary>
        private float accPerFrameAddCount;
        /// <summary>
        /// 对应decSlope的每帧增加向量模长
        /// </summary>
        private float decPerFrameDecCount;

        private float dotProduct = 0f;


        #endregion

        #endregion

        #region 总体模拟更新函数
        public void InputSimulateModeThenUpdateMode(SimulateMode mode)
        {
            simulateMode = mode;
        }

        /// <summary>
        /// 提供方法暴露simulate参数，避免参数被滥用
        /// </summary>
        public SimulateMode ExposeSimulateMode()
        {
            return simulateMode;
        }
        #endregion


        private void Start()
        {
            InitParameterAndWorning();

            if (UsingCurveMove || speedCurve == null) //自动生成运动曲线
                GennerCurveByPara();
        }

        //在Editor模式运行，用于更新参数
        private void OnValidate()
        {
            InitParameterAndWorning();
        }


        #region 开始内调函数
        void InitParameterAndWorning()//初始化参数
        {
            //应用速度比例
            cachedInitialInstantSpeed = initialInstantSpeed * SpeedScale;
            cachedMaxSpeed = maxSpeed * SpeedScale;
            cachedMinSpeed = minSpeed * SpeedScale;

            //进行速度参数相关运算
            velocityThresholdScaledQuar = Mathf.Pow(velocityThreshold * SpeedScale, 2);

            InverseSpeedScale = 1 / SpeedScale;

            accLinearSlope = cachedMaxSpeed / accFullSpeedTime;

            decLinearSlope = cachedMaxSpeed / decFullSpeedTime;

            accPerFrameAddCount = Time.fixedDeltaTime * accLinearSlope;

            decPerFrameDecCount = Time.fixedDeltaTime * decLinearSlope;
        }

        public void GennerCurveByPara()
        {
            speedCurve = new AnimationCurve(
                new Keyframe(0f, cachedMinSpeed),
                new Keyframe(accFullSpeedTime, cachedMaxSpeed),
                new Keyframe(endTime, cachedMinSpeed)
                );
#if UNITY_EDITOR
            Debug.Log($"2:{accFullSpeedTime}, 3: {endTime}");
#endif
            speedCurve.SmoothTangents(threeFrame.start_frameOffset, treeFrameSmoothWeight.start_strength);
            speedCurve.SmoothTangents(threeFrame.max_frameOffset, treeFrameSmoothWeight.maxtime_strength);
            speedCurve.SmoothTangents(threeFrame.end_frameOffset, treeFrameSmoothWeight.end_strength);

        }
        #endregion

        void Update()
        {
            Movement();
            //直接使用曲线进行速度的更新
        }

        #region 更新内调函数
        private void Movement()
        {
            if(simulateMode == SimulateMode.EightDirectionMode) //八向运动
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
            }
            else //二向运动
            {
                horizontal = Input.GetAxis("Horizontal");
                vertical = 0f;
            }

            direction = new Vector2(horizontal, vertical).normalized;
        }
        #endregion

        private void FixedUpdate()
        {
            //物理更新
            PhysicalUpdate();

        }

        #region FixedUpdate内调函数
        private void PhysicalUpdate()
        {
            //运动更新
            if (UsingCurveMove)
            {
                //运动更新:分为普通更新和曲线更行，使用曲线更新是才进行
                CurveMoveOnCachedVelocity();
            }
            else//使用普通更新，直接使用速度
            {
                DirectMoveOnCachedVelocity();
            }
            if (IsVelocityNeedThreshold())
            {
                cachedRbVelocity = Vector2.zero;
            }
            //根据cachedVelocity更新速度
            UpdateVelocity(cachedRbVelocity);
        }

        private void CurveMoveOnCachedVelocity()
        {
            if (cachedRbVelocity.Equals(Vector2.zero) && direction.Equals(Vector2.zero))//静止就退出，减少运算
                return;

            if (!cachedRbVelocity.Equals(Vector2.zero) && direction.Equals(Vector2.zero))//唯一处理的情况
            {
                if (IsVelocityNeedThreshold())//当数据极其小时
                {
                    cachedRbVelocity = Vector2.zero;
                    return;
                }
                Vector2 normalized = cachedRbVelocity.normalized;
                dotProduct = Vector2.Dot(cachedRbVelocity, normalized);
                cachedRbVelocity = dotProduct * normalized;
                dotProduct = 0f;
            }
            else
            {
                dotProduct = Vector2.Dot(cachedRbVelocity, direction);
                //对当前方向上的速度进行更新，使用点乘，除去垂直的速度，对更新的单一速度进行曲线匹配
                cachedRbVelocity = dotProduct * direction;
            }

            //后面进行前后段曲线的匹配：之前进行向量的投影，所以简化为考虑直线方向
            if (cachedRbVelocity.Equals(Vector2.zero))//当前静止 
            {
                if (direction.Equals(Vector2.zero))//完全静止
                {
                    return;
                }
                else//加速意愿，进行加速
                {
                    cachedRbVelocity = (cachedInitialInstantSpeed) * direction;
                    AccelerateAlongCurve();
                }
            }
            else//运动中
            {
                if (dotProduct <= 0)//方向相反，或者相同，并且速度没有为0，即为减速
                {

                    DecelerateAlongCurve();
                }
                else if (currentCachedSpeed < cachedMaxSpeed)//方向相同，并且没有达到最大速度。即为加速
                {

                    AccelerateAlongCurve();
                }
            }
        }

        /// <summary>
        /// 直接进行速度的更新
        /// </summary>
        private void DirectMoveOnCachedVelocity()
        {
            if(direction.Equals(Vector2.zero))
            {
                cachedRbVelocity = Vector2.zero;
                return;
            }
            cachedRbVelocity = direction * cachedMaxSpeed;
        }

        #endregion

        //曲线过于复杂，暂时使用直线进行
        #region 曲线迭代函数
        private void AccelerateAlongCurve()//简化曲线操作进行速度插值
        {
            cachedRbVelocity += accPerFrameAddCount * direction;
            ClampCachedVelocity();
        }

        private void DecelerateAlongCurve()//简化曲线操作进行速度插值
        {
            direction = cachedRbVelocity.normalized;
            cachedRbVelocity -= decPerFrameDecCount * direction;
            //添加速度截断
            VelocityThreshold();
            ClampCachedVelocity();
        }
        /// <summary>
        /// 在减速中进行速度截断，避免出现交替减速的情况
        /// </summary>
        private void VelocityThreshold()
        {
            if(cachedRbVelocity.sqrMagnitude <= velocityThresholdScaledQuar)
            {
                cachedRbVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// 进行更新避免出现超出的部分
        /// </summary>
        /// <returns></returns>
        private bool ClampCachedVelocity()
        {
            float clampedCurrentCachedSpeed = Mathf.Clamp(currentCachedSpeed, cachedMinSpeed, cachedMaxSpeed);
            cachedRbVelocity = clampedCurrentCachedSpeed * direction;
            return true;
        }

        private bool IsVelocityNeedThreshold()
        {
            return (cachedRbVelocity.sqrMagnitude < velocityPowThreshold);
        }
        #endregion

        #region 速度尺寸更新函数
        /// <summary>
        /// 只在需要应用时修改真正的rb.Velocity
        /// </summary>
        /// <param name="applyVector">需要映射到rb.Velocity的向量</param>
        /// <returns></returns>
        private bool UpdateVelocity(Vector2 applyVector)
        {
            rb.velocity = applyVector * InverseSpeedScale;
            return true;
        }
        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MoveControllerDropIn))]
    public class BoyControllerCustomInspectorEditor : Editor
    {
        private SerializedProperty curAnimationCurve;

        private MoveControllerDropIn moveController;

        private bool eightDirectionBool;

        private bool twoDirectionBool;

        private MoveControllerDropIn.SimulateMode bossSimulateMode;
    
        private void OnEnable()
        {
            curAnimationCurve = serializedObject.FindProperty("speedCurve");//获取SerializedProperty
            moveController = (MoveControllerDropIn)target;//target为[CustomEditor()]获取到的Object
            bossSimulateMode = moveController.ExposeSimulateMode();
            eightDirectionBool = (bossSimulateMode == MoveControllerDropIn.SimulateMode.EightDirectionMode);
            twoDirectionBool = !eightDirectionBool;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();//将对序列化属性进行更新

            EditorGUILayout.LabelField("脚本控制运行方式");
            EditorGUILayout.BeginHorizontal();
            bool new_eightDirectionBool = GUILayout.Toggle(eightDirectionBool, "八向运动");
            bool new_twoDirectionBool = GUILayout.Toggle(twoDirectionBool, "二向运动");
            EditorGUILayout.EndHorizontal();
            UpdateBoolAndSimulateMode(new_eightDirectionBool, new_twoDirectionBool); //进行更新

            base.OnInspectorGUI();//渲染父类的inspector

            EditorGUILayout.PropertyField(curAnimationCurve);//创建属性区域

            if (GUILayout.Button("生成运动曲线"))
            {
                moveController.GennerCurveByPara();
            }
            serializedObject.ApplyModifiedProperties();//应用更新
        }

        //更新选项的bool值和运动脚本的Mode值
        private void UpdateBoolAndSimulateMode(bool new_eight, bool new_two)
        {
            if (eightDirectionBool != new_eight)
            {
                eightDirectionBool = true;
                twoDirectionBool = false;
                moveController.InputSimulateModeThenUpdateMode(MoveControllerDropIn.SimulateMode.EightDirectionMode);
            }
            else if (twoDirectionBool != new_two)
            {
                twoDirectionBool = true;
                eightDirectionBool = false;
                moveController.InputSimulateModeThenUpdateMode(MoveControllerDropIn.SimulateMode.TwoDirectionMode);
            }
        }
    }
#endif
}

