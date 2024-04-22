using Unity.VisualScripting;
using UnityEngine;

public class bicycle_code : MonoBehaviour  
{
    //较多使用tmp细化更新的过程提高可读性

    ///////////////////////////////////////////////////////// wheels ///////////////////////////////////////////////////////////
    [Header("轮胎碰撞体和模型")]
    // 轮胎的碰撞体
    public WheelCollider coll_frontWheel;
    public WheelCollider coll_rearWheel;
    // 轮胎的模型
    public GameObject meshFrontWheel;
    public GameObject meshRearWheel;

    [Tooltip("是否使用运行状态可视化")]public bool usingVisualWheel = false;
    [Tooltip("是否使用提示信息")] public bool usingInformation = false;
    // 用来模拟前轮滞空  check isn't front wheel in air for front braking possibility
    bool isFrontWheelInAir = true;

    //////////////////////////////////////// Stifness, CoM(center of mass), crahsed /////////////////////////////////////////////////////////////
    
    //for stiffness counting when rear brake is on. Need that to lose real wheel's stiffness during time
    float stiffPowerGain = 0.0f;
    //用于CoM的移动    for CoM moving along and across bike. Pilot's CoM.
    float tmpMassShift = 0.0f;//Z值上的变化
    //是否翻车的参数 crashed status. To know when we need to desable controls because bike is too leaned.
    [Header("车辆失控相关参数")]
    public bool crashed = false;
    //翻车角度 01-02 侧翻 03-04 前后翻  there is angles when bike takes status crashed(too much lean, or too much stoppie/wheelie)
    public float crashAngle01;//crashed status is on if bike have more Z(side fall) angle than this												
    public float crashAngle02;//crashed status is on if bike have less Z(side fall) angle than this 												
    public float crashAngle03;//crashed status is on if bike have more X(front fall) angle than this 												
    public float crashAngle04;//crashed status is on if bike have more X(back fall) angle than this												

    //自行车的质心 define CoM of bike
    [Header("自行车质心参数")]
    public Transform CoM; //CoM object
    public float normalCoM; //正常质心相对y值的参数  normalCoM is for situation when script need to return CoM in starting position										
    public float CoMWhenCrahsed; //崩溃时的质心调整参数 we beed lift CoM for funny bike turning around when crahsed													

    //////////////////// "beauties" of visuals - some meshes for display visual parts of bike ////////////////////////////////////////////
    [Header("车辆减震器")]
    public Transform rearPendulumn; //后轮防震器  rear pendulumn
    public Transform steeringWheel; //车把手 wheel bar
    public Transform suspensionFront_down; //减震器的位置   lower part of front forge
    private int normalFrontSuspSpring; //声明正常的前轮弹簧状态   we need to declare it to know what is normal front spring state is
    private int normalRearSuspSpring; //声明正常的后轮弹簧状态，用于拓展后轮的弹簧状态 we need to declare it to know what is normal rear spring state is
    private bool forgeBlocked = true; // variable to lock front forge for front braking
    //变量用于锁定前锻造，以实现前制动
    //为什么我们需要forgeBlocked？ 
    //PhysX 3.3f wheelCollider有一个小bug - 它只能在车重为1600kg和4个轮子的情况下正常工作。 
    //如果您的车不是4个轮子或者质量不是1600而是400 - 那么您就有麻烦了。 
    //问题是当悬架弹簧完全压缩、拉伸或者轮子在帧之间进入地下时（最灾难性的情况），它会产生绝对的巨大力量，并将刚体推向天空。 
    //所以，我的解决方案是找到这个时刻，并让弹簧暂时变弱，然后恢复到正常状态

    private float baseDistance; //轮轴距 need to know distance between wheels - base. It's for wheelie compensate(dont want wheelie for long bikes)

    [Header("平滑限制转向角度曲线")]
    //根据速度使用曲线来平滑限制运行的角度，提供更好的模拟效果 we need to clamp wheelbar angle according the speed. it means - the faster bike rides the less angle you can rotate wheel bar
    public AnimationCurve wheelbarRestrictCurve = new AnimationCurve(new Keyframe(0f, 20f), new Keyframe(100f, 1f));//first number in Keyframe is speed, second is max wheelbar degree

    //根据速度限制角度的临时变量   temporary variable to restrict wheel angle according speed
    private float tempMaxWheelAngle;

    //高速时限制转动角度为0，但是这里不涉及 variable for cut off wheel bar rotation angle at high speed
    //private float wheelPossibleAngle = 0.0f;

    //for wheels vusials match up the wheelColliders
    private Vector3 wheelCCenter;
    private RaycastHit hit;

    /////////////////////////////////////////// technical variables ///////////////////////////////////////////////////////
    [Header("运行直接控制参数")]
    public float frontBrakePower; //前轮刹车力度(抽象无实际含义)  brake power absract - 100 is good brakes																		

    public float LegsPower; //踏板的力量 Leg's power to wheels. Abstract it's not HP or KW or so...																	
    
    // airRes is for wind resistance to large bikes more than small ones
    public float airRes; //空气阻力，与车辆体型相关  Air resistant 																										// 1 is neutral

    private GameObject ctrlHub;//挂载总控台的物体 gameobject with script control variables 
    private controlHub outsideControls;//总控台脚本 making a link to corresponding bike's script
                                       /////////////////////////////////////////////////// BICYCLE CODE ///////////////////////////////////////////////////////
    private float frontWheelAPD;//静止车轮的底部测量的悬架和轮胎力的作用点距离 usualy 0.05f
    private GameObject pedals;
    private pedalControls linkToStunt;//pedalControls脚本用于特技
    private bool rearPend;

    private Rigidbody bicycleRigidbody;//避免重复计算


    [HideInInspector]
    public float bikeSpeed; //车辆速度km/h to know bike speed km/h
    public bool isReverseOn = false; //用于控制车辆后退 to turn On and Off reverse speed
    ////////////////////////////////////////////////  ON SCREEN INFO ///////////////////////////////////////////////////////
    //屏幕GUI绘制
    void OnGUI()
    {
        //定义屏幕GUI绘制的不同字体大小
        GUIStyle biggerText = new GUIStyle("label");
        biggerText.fontSize = 50;
        GUIStyle middleText = new GUIStyle("label");
        middleText.fontSize = 22;
        GUIStyle smallerText = new GUIStyle("label");
        smallerText.fontSize = 14;

        //在屏幕上显示速度 to show speed on display interface
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width * 0.875f, Screen.height * 0.9f, 120, 80), string.Format("" + "{0:0.}", bikeSpeed), biggerText);

        if (usingInformation)
        {
            if (!isReverseOn)
            {
                GUI.color = Color.grey;
                GUI.Label(new Rect(Screen.width * 0.885f, Screen.height * 0.96f, 60, 40), "REAR", smallerText);
            }
            else
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(Screen.width * 0.885f, Screen.height * 0.96f, 60, 40), "REAR", smallerText);
            }

            // user info help lines
            GUI.color = Color.black;
            GUI.Box(new Rect(10, 10, 180, 20), "A,W,S,D - main control", smallerText);

            GUI.Box(new Rect(10, 40, 120, 20), "X - rear brake", smallerText);
            GUI.Box(new Rect(10, 55, 320, 20), "Q,E,F,V - shift center of mass of biker", smallerText);
            GUI.Box(new Rect(10, 70, 320, 20), "R - restart / RightShift+R - full restart", smallerText);
            GUI.Box(new Rect(10, 85, 180, 20), "RMB - rotate camera around", smallerText);
            GUI.Box(new Rect(10, 115, 320, 20), "C - toggle reverse", smallerText);

            GUI.Box(new Rect(10, 130, 320, 20), "Space - bunnyhop", smallerText);
            GUI.Box(new Rect(10, 145, 320, 20), "M - turn left 180", smallerText);
            GUI.Box(new Rect(10, 160, 320, 20), "N - backflip 360", smallerText);
            GUI.Box(new Rect(10, 175, 220, 20), "2 - manual", smallerText);
            GUI.Box(new Rect(10, 190, 220, 20), "B - bunny jump right", smallerText);
            GUI.Box(new Rect(10, 205, 220, 20), "/ - 1 hard clutch for half second", smallerText);

            GUI.Box(new Rect(10, 220, 320, 20), "Esc - return to main menu", smallerText);
            GUI.color = Color.black;
        }
    }
    void Start()
    {
        //设置是否启用后轮悬挂
        //if there is no pendulum linked to script in Editor, it means MTB have no rear suspension, so no movement of rear wheel(pendulum)
        if (rearPendulumn)
        {
            rearPend = true;
        }
        else rearPend = false;

        //前轮悬架和轮胎作用点距离  bicycle code
        frontWheelAPD = coll_frontWheel.forceAppPointDistance;
        //通过名字来找到挂载ctrlHub的物体
        ctrlHub = GameObject.Find("gameScenario");//link to GameObject with script "controlHub"
        outsideControls = ctrlHub.GetComponent<controlHub>();//获取对应的ctrlHub脚本  to connect c# mobile control script to this one
        //同上获取对应的pedalControls物体和脚本
        pedals = GameObject.Find("bicycle_pedals");
        linkToStunt = pedals.GetComponent<pedalControls>();

        //获取当前挂载的刚体
        bicycleRigidbody = GetComponent<Rigidbody>();
        //设置自行车刚体的质心和惯性张量
        Vector3 setInitialTensor = bicycleRigidbody.inertiaTensor;//this string is necessary for Unity 5.3f with new PhysX feature when Tensor decoupled from center of mass
        bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);// now Center of Mass(CoM) is alligned to GameObject "CoM"
        bicycleRigidbody.inertiaTensor = setInitialTensor;////this string is necessary for Unity 5.3f with new PhysX feature when Tensor decoupled from center of mass

        //运行可视化 wheel colors for understanding of accelerate, idle, brake(white is idle status)
        if (usingVisualWheel)
        {
            meshFrontWheel.GetComponent<Renderer>().material.color = Color.black;
            meshRearWheel.GetComponent<Renderer>().material.color = Color.black;
        }


        //for better physics of fast moving bodies
        bicycleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        //应用一个比例来进行实际效果的修正 too keep LegsPower variable like "real" horse powers
        LegsPower = LegsPower * 20;

        //前轮制动力量  *30 is for good braking to keep frontBrakePower = 100 for good brakes. So, 100 is like sportsbike's Brembo
        frontBrakePower = frontBrakePower * 30;//30 is abstract but necessary for Unity5

        //运行参数，前后悬挂弹性系数 tehcnical variables
        normalRearSuspSpring = (int)coll_rearWheel.suspensionSpring.spring;
        normalFrontSuspSpring = (int)coll_frontWheel.suspensionSpring.spring;

        //轮轴距
        baseDistance = coll_frontWheel.transform.localPosition.z - coll_rearWheel.transform.localPosition.z;// now we know distance between two wheels

        //根据当前悬挂距离调整前后轮胎模型的y值(减震器之类的)
        //当前设置中前轮的减震性能更好，且后轮没使用suspension进行弥补
        var tmpMeshRWh01 = meshRearWheel.transform.localPosition;
        tmpMeshRWh01.y = meshRearWheel.transform.localPosition.y - coll_rearWheel.suspensionDistance / 4;
        meshRearWheel.transform.localPosition = tmpMeshRWh01;

        //纠正模型设置中后轮的位置
        //and bike's frame direction
        var tmpCollRW01 = coll_rearWheel.transform.localPosition;
        tmpCollRW01.y = coll_rearWheel.transform.localPosition.y - coll_rearWheel.transform.localPosition.y / 20;
        coll_rearWheel.transform.localPosition = tmpCollRW01;

    }
    void FixedUpdate()
    {
        //分别根据碰撞体更新前后轮模型的位置
        ApplyLocalPositionToVisuals(coll_frontWheel);
        ApplyLocalPositionToVisuals(coll_rearWheel);


        //////////////////////////////////// part where rear pendulum, wheelbar and wheels meshes matched to wheelsColliers and so on
        //beauty - rear pendulumn is looking at rear wheel(if you have both suspension bike)
        if (rearPend)
        {//有后悬挂的情况，对后轮弹簧的轮胎前后进行处理(改变X值)  rear pendulum moves only when bike is full suspension
            var tmp_cs1 = rearPendulumn.transform.localRotation;
            var tmp_cs2 = tmp_cs1.eulerAngles;
            tmp_cs2.x = 0 - 8 + (meshRearWheel.transform.localPosition.y * 100);
            tmp_cs1.eulerAngles = tmp_cs2;
            rearPendulumn.transform.localRotation = tmp_cs1;
        }
        //根据轮胎模型设置front_downy值，根据front_downz值设置轮胎模型z值  beauty - wheel bar rotating by front wheel
        var tmp_cs3 = suspensionFront_down.transform.localPosition;
        tmp_cs3.y = (meshFrontWheel.transform.localPosition.y - 0.15f);//与轮胎实际的设置向匹配
        suspensionFront_down.transform.localPosition = tmp_cs3; //设置front_down的相对的y值
        var tmp_cs4 = meshFrontWheel.transform.localPosition;
        tmp_cs4.z = meshFrontWheel.transform.localPosition.z - (suspensionFront_down.transform.localPosition.y + 0.4f) / 5;
        meshFrontWheel.transform.localPosition = tmp_cs4;//同时根据front_down的Z值来修正轮胎模型的位置(Z值)

        //可视化运行，用于调试 debug - all wheels are white in idle(no accelerate, no brake)
        if (usingVisualWheel)
        {
            meshFrontWheel.GetComponent<Renderer>().material.color = Color.black;
            meshRearWheel.GetComponent<Renderer>().material.color = Color.black;
        }


        //根据自行车的速度来动态调整空气阻力(drag代表线性阻力，angularDrag代表角阻力) drag and angular drag for emulate air resistance
        if (!crashed)
        {
            bicycleRigidbody.drag = bicycleRigidbody.velocity.magnitude / 210 * airRes; // when 250 bike can easy beat 200km/h // ~55 m/s
            bicycleRigidbody.angularDrag = 7 + bicycleRigidbody.velocity.magnitude / 20;
        }

        //将速度从m/s转未km/h，先乘10利于后面取整     determinate the bike speed in km/h
        bikeSpeed = Mathf.Round((bicycleRigidbody.velocity.magnitude * 3.6f) * 10) * 0.1f; //from m/s to km/h

        ///根据速度调整frontWheelAPD从而影响转弯时的倾斜角，更好地模拟现实的情况  bicycle code
        coll_frontWheel.forceAppPointDistance = frontWheelAPD - bikeSpeed / 1000;//速度越大越小
        if (coll_frontWheel.forceAppPointDistance < 0.001f)//限定最小的车架与受力点距离
        {
            coll_frontWheel.forceAppPointDistance = 0.001f;
        }

        //////////////////////////////////// acceleration & brake /////////////////////////////////////////////////////////////
        //////////////////////////////////// ACCELERATE /////////////////////////////////////////////////////////////
        //自行车运动控制
        if (!crashed && outsideControls.Vertical > 0 && !isReverseOn)//前进时
        {//case with acceleration from 0.0f to 0.9f throttle
            coll_frontWheel.brakeTorque = 0;//避免刹车后加速出现的bug  we need that to fix strange unity bug when bike stucks if you press "accelerate" just after "brake".
            coll_rearWheel.motorTorque = LegsPower * outsideControls.Vertical;//设置车轮的马力矩

            //可视化运行，用于调试 debug - rear wheel is green when accelerate
            if (usingVisualWheel)
            {
                meshRearWheel.GetComponent<Renderer>().material.color = Color.green;

            }
            

            //更新自行车刚体的质心位置 when normal accelerating CoM z is averaged
            var tmp_cs5 = CoM.localPosition;
            tmp_cs5.z = 0.0f + tmpMassShift;
            tmp_cs5.y = normalCoM;
            CoM.localPosition = tmp_cs5;
            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }
        //case for reverse
        if (!crashed && outsideControls.Vertical > 0 && isReverseOn)//后退时
        {
            coll_rearWheel.motorTorque = LegsPower * -outsideControls.Vertical / 2 + (bikeSpeed * 50);//进行运动力量的变换，从前进马力矩数值到后退的  need to make reverse really slow

            //可视化运行，用于调试 debug - rear wheel is green when accelerate
            if (usingVisualWheel)
                meshRearWheel.GetComponent<Renderer>().material.color = Color.green;

            //更新自行车刚体的质心位置 when normal accelerating CoM z is averaged
            var tmp_cs6 = CoM.localPosition;
            tmp_cs6.z = 0.0f + tmpMassShift;
            tmp_cs6.y = normalCoM;
            CoM.localPosition = tmp_cs6;
            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }

        //////////////////////////////////// ACCELERATE 'full throttle - manual' ///////////////////////////////////////////////////////
        //进行manual特技时进行质心位置的补偿，更好地模拟物理效果（设定为vertical>0.9即为进行manual特技）
        if (!crashed && outsideControls.Vertical > 0.9f && !isReverseOn)// acceleration >0.9f throttle for wheelie	
        {

            //动力更新
            coll_frontWheel.brakeTorque = 0;//避免刹车后加速出现的bug   we need that to fix strange unity bug when bike stucks if you press "accelerate" just after "brake".
            coll_rearWheel.motorTorque = LegsPower * 1.2f; // 1.2f mean it's full throttle



            if(usingVisualWheel)
                meshRearWheel.GetComponent<Renderer>().material.color = Color.green;
            bicycleRigidbody.angularDrag = 20;//设置角阻力用于前轮离地稳定性 for wheelie stability

            //设置运算参数调整质心位置(z值)，为了实现更好地抬车头特技效果（涉及tmpMassShift）
            CoM.localPosition = new Vector3(CoM.localPosition.z, CoM.localPosition.y, -(1.38f - baseDistance / 1.4f) + tmpMassShift);
            //still working on best wheelie code

            float stoppieEmpower = (bikeSpeed / 3) / 100; //设置权重参数放置前路离地时倒地
            // need to supress wheelie when leaning because it's always fall and it't not fun at all
            //角度倾斜补偿
            float angleLeanCompensate = 0.0f;
            if (this.transform.localEulerAngles.z < crashAngle01)
            {
                angleLeanCompensate = this.transform.localEulerAngles.z / 30;
                if (angleLeanCompensate > 0.5f)
                {
                    angleLeanCompensate = 0.5f;
                }
            }
            if (this.transform.localEulerAngles.z > crashAngle02)
            {
                angleLeanCompensate = (360 - this.transform.localEulerAngles.z) / 30;
                if (angleLeanCompensate > 0.5f)
                {
                    angleLeanCompensate = 0.5f;
                }
            }
            //当速度过大、倾斜角度过大，设置stoppieEmpower
            if (stoppieEmpower + angleLeanCompensate > 0.5f)
            {
                stoppieEmpower = 0.5f;
            }
            //根据stoppieEmpower设置CoM的y值，在速度过大、倾斜角度大时模拟质心减低的物理过程
            CoM.localPosition = new Vector3(CoM.localPosition.x, -(0.995f - baseDistance / 2.8f) - stoppieEmpower, CoM.localPosition.z);
            //同时更新刚体的质心
            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);

            //this is attenuation for rear suspension targetPosition
            //I've made it to prevent very strange launch to sky when wheelie in new Phys3
            var tmpSpsSprg01 = coll_rearWheel.suspensionSpring;//dumper for wheelie jumps
            tmpSpsSprg01.spring = 200000;
            coll_rearWheel.suspensionSpring = tmpSpsSprg01;

        }
        else RearSuspensionRestoration();


        //////////////////////////////////// BRAKING /////////////////////////////////////////////////////////////
        //////////////////////////////////// front brake /////////////////////////////////////////////////////////
        //前轮刹车
        //我的解决方案是找到这个时刻，并让弹簧暂时变弱，然后恢复到正常状态
        int springWeakness = 0;
        if (!crashed && outsideControls.Vertical < 0 && !isFrontWheelInAir)
        {

            //动力更新
            coll_frontWheel.brakeTorque = frontBrakePower * -outsideControls.Vertical;
            coll_rearWheel.motorTorque = 0; //避免同时出现加速和刹车 you can't do accelerate and braking same time.


            //more user firendly gomeotric progession braking. But less stoppie and fun :( Boring...

            if (bikeSpeed > 1)
            {// no CoM pull up when speed is zero

                //现实中使用后刹有利于停车，在前刹之前使用后刹会导致自行车部分拉伸 when rear brake is used it helps a little to prevent stoppie. Because in real life bike "stretch" a little when you using rear brake just moment before front.
                float rearBrakeAddon = 0.0f;
                if (outsideControls.rearBrakeOn)//后刹
                {
                    rearBrakeAddon = 0.0025f;
                }
                //操作质心模拟现实中自行车被部分拉伸的物理过程
                var tmp_cs11 = CoM.localPosition;
                tmp_cs11.y += (frontBrakePower / 200000) + tmpMassShift / 50f - rearBrakeAddon;
                tmp_cs11.z += 0.0025f;
                CoM.localPosition = tmp_cs11;

            }
            else if (bikeSpeed <= 1 && !crashed && this.transform.localEulerAngles.z < 45 || bikeSpeed <= 1 && !crashed && this.transform.localEulerAngles.z > 315)
            {
                if (this.transform.localEulerAngles.x < 5 || this.transform.localEulerAngles.x > 355)//当速度很小，偏转角很小时重置质心位置
                {
                    var tmp_cs12 = CoM.localPosition;
                    tmp_cs12.y = normalCoM;
                    CoM.localPosition = tmp_cs12;
                }
            }
            //限制Com的y值
            if (CoM.localPosition.y >= -0.2f)
            {
                var tmp_cs13 = CoM.localPosition;
                tmp_cs13.y = -0.2f;
                CoM.localPosition = tmp_cs13;
            }
            //限制CoM的z值
            if (CoM.localPosition.z >= 0.2f + (bicycleRigidbody.mass / 500))
            {
                CoM.localPosition = new Vector3(CoM.localPosition.x, 0.2f + (bicycleRigidbody.mass / 500), CoM.localPosition.z);
            }

            //////////// 
            //this is attenuation for front suspension when forge spring is compressed
            //I've made it to prevent very strange launch to sky when wheelie in new Phys3
            //problem is launch bike to sky when spring must expand from compressed state. In real life front forge can't create such force.
            float maxFrontSuspConstrain;//最大前悬置约束，限制回弹时的力  temporary variable to make constrain for attenuation ususpension(need to make it always ~15% of initial force) 
            maxFrontSuspConstrain = CoM.localPosition.z;
            if (maxFrontSuspConstrain >= 0.5f) maxFrontSuspConstrain = 0.5f;
            springWeakness = (int)(normalFrontSuspSpring - (normalFrontSuspSpring * 1.5f) * maxFrontSuspConstrain);


            
            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
            // debug - wheel is red when braking
            if(usingVisualWheel)
                meshFrontWheel.GetComponent<Renderer>().material.color = Color.red;

            //we need to mark suspension as very compressed to make it weaker
            forgeBlocked = true;
        }
        else FrontSuspensionRestoration(springWeakness);//限制前轮悬置弹性，并逐渐回弹  here is function for weak front spring and return it's force slowly


        //////////////////////////////////// rear brake /////////////////////////////////////////////////////////
        // rear brake - it's all about lose side stiffness more and more till rear brake is pressed
        //后刹
        if (!crashed && outsideControls.rearBrakeOn)
        {
            //更新动力
            coll_rearWheel.brakeTorque = frontBrakePower / 2;//动力没有前轮强

            if (this.transform.localEulerAngles.x > 180 && this.transform.localEulerAngles.x < 350)
            {
                //更新质心
                var tmp_cs14 = CoM.localPosition;
                tmp_cs14.z = 0.0f + tmpMassShift;
                CoM.localPosition = tmp_cs14;
            }

            coll_frontWheel.forceAppPointDistance = 0.25f;//让前轮更容易打滑

            stiffPowerGain = stiffPowerGain += 0.025f - (bikeSpeed / 10000);
            if (stiffPowerGain > 0.9f - bikeSpeed / 300) { 
                stiffPowerGain = 0.9f - bikeSpeed / 300;
            }

            //更新质心
            var tmp_cs15a = CoM.localPosition;
            tmp_cs15a.z = tmp_cs15a.z += 0.05f;
            CoM.localPosition = tmp_cs15a;

            //限制质心
            if (CoM.localPosition.z >= 0.5f)
            {
                var tmp_cs15b = CoM.localPosition;
                tmp_cs15b.z = 0.5f;
                CoM.localPosition = tmp_cs15b;

            }
            //设置后轮的侧滑摩檫力（减少产生侧滑）
            var tmp_cs15z = coll_rearWheel.sidewaysFriction;
            tmp_cs15z.stiffness = 0.9f - stiffPowerGain;// (2 - for stability, 0.01f - falls in a moment)
            coll_rearWheel.sidewaysFriction = tmp_cs15z;

            if(usingVisualWheel)
                meshRearWheel.GetComponent<Renderer>().material.color = Color.red;

        }
        else
        {

            coll_rearWheel.brakeTorque = 0;

            stiffPowerGain = stiffPowerGain -= 0.05f;
            if (stiffPowerGain < 0)
            {
                stiffPowerGain = 0;
            }

            //重置两个轮胎的侧滑摩檫力
            var tmp_cs17 = coll_rearWheel.sidewaysFriction;
            tmp_cs17.stiffness = 1.0f - stiffPowerGain;
            coll_rearWheel.sidewaysFriction = tmp_cs17;// side stiffness is back to 2
            var tmp_cs18 = coll_frontWheel.sidewaysFriction;
            tmp_cs18.stiffness = 1.0f - stiffPowerGain;
            coll_frontWheel.sidewaysFriction = tmp_cs18;// side stiffness is back to 1
        }


        //////////////////////////////////// reverse /////////////////////////////////////////////////////////
        //反转前进后退
        if (!crashed && outsideControls.reverse && bikeSpeed <= 0)
        {
            outsideControls.reverse = false;
            if (isReverseOn == false)
            {
                isReverseOn = true;
            }
            else isReverseOn = false;
        }


        //////////////////////////////////// turnning /////////////////////////////////////////////////////////////		
        //进行运动时转向角度的限制

        // there is MOST trick in the code
        // the Unity physics isn't like real life. Wheel collider isn't round as real bike tyre.
        // so, face it - you can't reach accurate and physics correct countersteering effect on wheelCollider
        // For that and many other reasons we restrict front wheel turn angle when when speed is growing
        //(honestly, there was a time when MotoGP bikes has restricted wheel bar rotation angle by 1.5f degree ! as we got here :)			
        tempMaxWheelAngle = wheelbarRestrictCurve.Evaluate(bikeSpeed);//通过速度获取角度限制    associate speed with curve which you've tuned in Editor

        if (!crashed && outsideControls.Horizontal != 0)
        {

            // while speed is high, wheelbar is restricted 
            coll_frontWheel.steerAngle = tempMaxWheelAngle * outsideControls.Horizontal;
            steeringWheel.rotation = coll_frontWheel.transform.rotation * Quaternion.Euler(0, coll_frontWheel.steerAngle, coll_frontWheel.transform.rotation.z);//对车把手模型进行旋转
        }
        else coll_frontWheel.steerAngle = 0;


        /////////////////////////////////////////////////// PILOT'S MASS //////////////////////////////////////////////////////////
        // it's part about moving of pilot's center of mass. It can be used for wheelie or stoppie control and for motocross section in future
        //not polished yet. For mobile version it should back pilot's mass smooth not in one tick
        //处理骑车手的质量
        if (outsideControls.VerticalMassShift > 0)
        {
            //更新质心
            tmpMassShift = outsideControls.VerticalMassShift / 12.5f; //12.5f to get 0.08fm at final
            var tmp_cs19 = CoM.localPosition;
            tmp_cs19.z = tmpMassShift;
            CoM.localPosition = tmp_cs19;

            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }
        if (outsideControls.VerticalMassShift < 0)
        {
            //更新质心
            tmpMassShift = outsideControls.VerticalMassShift / 12.5f;//12.5f to get 0.08fm at final
            var tmp_cs20 = CoM.localPosition;
            tmp_cs20.z = tmpMassShift;
            CoM.localPosition = tmp_cs20;

            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }
        if (outsideControls.HorizontalMassShift < 0)
        {
            //更新质心
            var tmp_cs21 = CoM.localPosition;
            tmp_cs21.x = outsideControls.HorizontalMassShift / 40;
            CoM.localPosition = tmp_cs21;//40 to get 0.025m at final

            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);

        }
        if (outsideControls.HorizontalMassShift > 0)
        {
            //更新质心(x轴上)
            var tmp_cs22 = CoM.localPosition;
            tmp_cs22.x = outsideControls.HorizontalMassShift / 40;
            CoM.localPosition = tmp_cs22;//40 to get 0.025m at final

            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }


        //不按按钮时所有的量重置  auto back CoM when any key not pressed
        if (!crashed && outsideControls.Vertical == 0 && !outsideControls.rearBrakeOn && !linkToStunt.stuntIsOn || (outsideControls.Vertical < 0 && isFrontWheelInAir))
        {
            var tmp_cs23 = CoM.localPosition;
            tmp_cs23.y = normalCoM;
            tmp_cs23.z = 0.0f + tmpMassShift;
            CoM.localPosition = tmp_cs23;
            coll_frontWheel.motorTorque = 0;
            coll_frontWheel.brakeTorque = 0;
            coll_rearWheel.motorTorque = 0;
            coll_rearWheel.brakeTorque = 0;
            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }
        //重置骑车手的质心(z轴)偏移   autoback pilot's CoM along
        if (outsideControls.VerticalMassShift == 0 && outsideControls.Vertical >= 0 && outsideControls.Vertical <= 0.9f && !outsideControls.rearBrakeOn && !linkToStunt.stuntIsOn)
        {
            var tmp_cs24 = CoM.localPosition;
            tmp_cs24.z = 0.0f;
            CoM.localPosition = tmp_cs24;
            tmpMassShift = 0.0f;
        }
        //autoback pilot's CoM across
        //重置骑车手的质心(x轴)偏移 
        if (outsideControls.HorizontalMassShift == 0 && outsideControls.Vertical <= 0 && !outsideControls.rearBrakeOn)
        {
            var tmp_cs25 = CoM.localPosition;
            tmp_cs25.x = 0.0f;
            CoM.localPosition = tmp_cs25;
        }

        /////////////////////////////////////////////////////// RESTART KEY ///////////////////////////////////////////////////////////
        //重新恢复姿态的按键
        // Restart key - recreate bike few meters above current place
        if (outsideControls.restartBike)
        {
            if (outsideControls.fullRestartBike)
            {
                transform.position = new Vector3(0, 1, -11);
                transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            }
            //重置崩溃参数
            crashed = false;
            //重置位置和角度
            transform.position += new Vector3(0, 0.1f, 0);
            transform.rotation = Quaternion.Euler(0.0f, transform.localEulerAngles.y, 0.0f);
            //重置速度和角速度
            bicycleRigidbody.velocity = Vector3.zero;
            bicycleRigidbody.angularVelocity = Vector3.zero;
            //重置质心位置和刚体质心
            var tmp_cs26 = CoM.localPosition;
            tmp_cs26.x = 0.0f;
            tmp_cs26.y = normalCoM;
            tmp_cs26.z = 0.0f;
            CoM.localPosition = tmp_cs26;
            //for fix bug when front wheel IN ground after restart(sorry, I really don't understand why it happens);
            coll_frontWheel.motorTorque = 0;
            coll_frontWheel.brakeTorque = 0;
            coll_rearWheel.motorTorque = 0;
            coll_rearWheel.brakeTorque = 0;
            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }



        ///////////////////////////////////////// CRASH happens /////////////////////////////////////////////////////////
        // conditions when crash is happen
        //发生崩溃的逻辑
        if ((this.transform.localEulerAngles.z >= crashAngle01 && this.transform.localEulerAngles.z <= crashAngle02) && !linkToStunt.stuntIsOn || (this.transform.localEulerAngles.x >= crashAngle03 && this.transform.localEulerAngles.x <= crashAngle04 && !linkToStunt.stuntIsOn))
        {
            bicycleRigidbody.drag = 0.1f; // when 250 bike can easy beat 200km/h // ~55 m/s
            bicycleRigidbody.angularDrag = 0.01f;
            crashed = true;
            var tmp_cs27 = CoM.localPosition;
            tmp_cs27.x = 0.0f;
            tmp_cs27.y = CoMWhenCrahsed;//move CoM a little bit up for funny bike rotations when fall
            tmp_cs27.z = 0.0f;
            CoM.localPosition = tmp_cs27;
            bicycleRigidbody.centerOfMass = new Vector3(CoM.localPosition.x, CoM.localPosition.y, CoM.localPosition.z);
        }

        if (crashed) coll_rearWheel.motorTorque = 0;//防止崩溃时仍然可以加速而出现bug
    }

    ///////////////////////////////////////////// FUNCTIONS /////////////////////////////////////////////////////////

    void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = collider.transform.GetChild(0);
        wheelCCenter = collider.transform.TransformPoint(collider.center);//获取collider的世界空间位置


        //dpn't need movement of rear suspension because MTB have no rear suspension
        if (!rearPend)
        {//没有后悬挂的情况，悬挂方面只用处理前轮的模型，但是后轮物理悬挂仍会作用 case where MTB have no rear suspension
            if (collider.gameObject.name != "coll_rear_wheel")
            {//是前轮的情况，进行射线检测
                if (Physics.Raycast(wheelCCenter, -collider.transform.up, out hit, collider.suspensionDistance + collider.radius))//悬挂起作用的时候
                {
                    visualWheel.transform.position = hit.point + (collider.transform.up * collider.radius);//前车轮模型在对应的位置
                    if (collider.name == "coll_front_wheel") isFrontWheelInAir = false;//说明自行车前轮未悬空
                }
                else//悬挂未起作用时，在空中
                {
                    visualWheel.transform.position = wheelCCenter - (collider.transform.up * collider.suspensionDistance);//前车轮模型在对应碰撞体减去悬挂距离的位置
                    if (collider.name == "coll_front_wheel") isFrontWheelInAir = true;
                }
            }
        }
        else
        {//有后悬挂的情况，前后轮模型更新逻辑一致   case where bicycle has sull suspension
            if (Physics.Raycast(wheelCCenter, -collider.transform.up, out hit, collider.suspensionDistance + collider.radius))
            {
                visualWheel.transform.position = hit.point + (collider.transform.up * collider.radius);
                if (collider.name == "coll_front_wheel") isFrontWheelInAir = false;

            }
            else
            {
                visualWheel.transform.position = wheelCCenter - (collider.transform.up * collider.suspensionDistance);
                if (collider.name == "coll_front_wheel") isFrontWheelInAir = true;
            }

        }

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        collider.GetWorldPose(out position, out rotation);

        //更新转向轮(前轮)的模型偏角度，相对角度进行旋转;collider.steerAngle即为碰撞体转向角；后轮不会更新
        visualWheel.localEulerAngles = new Vector3(visualWheel.localEulerAngles.x, collider.steerAngle - visualWheel.localEulerAngles.z, visualWheel.localEulerAngles.z);
        //更新模型的转动情况
        visualWheel.Rotate(collider.rpm / 60 * 360 * Time.deltaTime, 0.0f, 0.0f);//rpm只读属性，车轮每分钟转速

    }
    //need to restore spring power for rear suspension after make it harder for wheelie
    void RearSuspensionRestoration()
    {
        var tmpRearSusp = coll_rearWheel.suspensionSpring;
        tmpRearSusp.spring = normalRearSuspSpring;
        coll_rearWheel.suspensionSpring = tmpRearSusp;   
    }
    //need to restore spring power for front suspension after make it weaker for stoppie
    void FrontSuspensionRestoration(int sprWeakness)
    {
        if (forgeBlocked)
        {//减弱前轮的suspensionSpring悬置弹性，避免返回过大的力  supress front spring power to avoid too much force back
            var tmpFrntSusp = coll_frontWheel.suspensionSpring;
            tmpFrntSusp.spring = sprWeakness;
            coll_frontWheel.suspensionSpring = tmpFrntSusp;
            forgeBlocked = false;
        }
        if (coll_frontWheel.suspensionSpring.spring < normalFrontSuspSpring)
        {//逐渐返还削弱的悬置弹性   slowly returning force to front spring
            var tmpFrntSusp2 = coll_frontWheel.suspensionSpring;
            tmpFrntSusp2.spring += 500.0f;
            coll_frontWheel.suspensionSpring = tmpFrntSusp2;
        }
    }
}