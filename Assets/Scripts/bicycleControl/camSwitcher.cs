using UnityEngine;
using System.Collections;

public class camSwitcher : MonoBehaviour
{

    public Camera backCamera; // 后置摄像机
    public Camera aroundCamera; // 周围摄像机
    public Transform cameraTarget; // 摄像机目标
    private Camera currentCamera; // 当前摄像机
    //////////////////// 用于后置摄像机
    float dist = 3.0f; // 距离
    float height = 1.0f; // 高度
    //////////////////// 用于周围摄像机
    private float distance = 3.0f; // 距离
    private float xSpeed = 10.0f; // X轴旋转速度
    private float ySpeed = 10.0f; // Y轴旋转速度
    
    private float yMinLimit = -90; // Y轴最小旋转角度
    private float yMaxLimit = 90; // Y轴最大旋转角度
    
    private float distanceMin = 2; // 最小距离
    private float distanceMax = 10; // 最大距离
    
    private float x = 0.0f; // X轴旋转角度
    private float y = 0.0f; // Y轴旋转角度
    
    private float smoothTime = 0.2f; // 平滑时间
    
    private float xSmooth = 0.0f; // 平滑后的X轴旋转角度
    private float ySmooth = 0.0f; // 平滑后的Y轴旋转角度
    private float xVelocity = 0.0f; // X轴旋转速度
    private float yVelocity = 0.0f; // Y轴旋转速度

    // 新的摄像机行为
    private float currentTargetAngle; // 当前目标角度
    
    private GameObject ctrlHub; // 包含控制变量的游戏对象
    private controlHub outsideControls; // 将链接到对应自行车脚本的链接
    
    // 初始化
    void Start ()
    {
        ctrlHub = GameObject.Find("gameScenario"); // 链接到包含“controlHub”脚本的游戏对象
        outsideControls = ctrlHub.GetComponent<controlHub>(); // 将C#移动控制脚本连接到此脚本上

        backCamera.enabled = true; // 启用后置摄像机
        aroundCamera.enabled = false; // 禁用周围摄像机
        currentCamera = backCamera; // 当前摄像机为后置摄像机
        
        if (GetComponent<Rigidbody> ()) GetComponent<Rigidbody> ().freezeRotation = true; // 冻结刚体旋转
    
        currentTargetAngle = cameraTarget.transform.eulerAngles.z; // 当前目标角度为摄像机目标的Z轴旋转角度
    }
    
    // 每帧更新
    void LateUpdate ()
    {
#if UNITY_STANDALONE || UNITY_WEBPLAYER // 仅为移动设备启用摄像机旋转，以便在任意位置进行触摸
        if (Input.GetMouseButton (1)) {

            backCamera.enabled = false; // 禁用后置摄像机
            aroundCamera.enabled = true; // 启用周围摄像机
            backCamera.gameObject.SetActive (false); // 禁用后置摄像机游戏对象
            aroundCamera.gameObject.SetActive (true); // 启用周围摄像机游戏对象
            currentCamera = aroundCamera; // 当前摄像机为周围摄像机
            
            
            x += Input.GetAxis ("Mouse X") * xSpeed; // 根据鼠标X轴移动调整X轴旋转角度
            y -= Input.GetAxis ("Mouse Y") * ySpeed; // 根据鼠标Y轴移动调整Y轴旋转角度
            
            y = Mathf.Clamp (y, yMinLimit, yMaxLimit); // 限制Y轴旋转角度的范围
            
            
            
            xSmooth = Mathf.SmoothDamp (xSmooth, x, ref xVelocity, smoothTime); // 平滑X轴旋转角度
            ySmooth = Mathf.SmoothDamp (ySmooth, y, ref yVelocity, smoothTime); // 平滑Y轴旋转角度
            
            
            distance = Mathf.Clamp (distance + Input.GetAxis ("Mouse ScrollWheel") * distance, distanceMin, distanceMax); // 根据鼠标滚轮调整距离
            
            currentCamera.transform.localRotation = Quaternion.Euler (ySmooth, xSmooth, 0); // 设置摄像机的旋转角度
            currentCamera.transform.position = currentCamera.transform.rotation * new Vector3 (0.0f, 0.0f, -distance) + cameraTarget.position; // 设置摄像机的位置


        } else {
#endif
            backCamera.enabled = true; // 启用后置摄像机
            aroundCamera.enabled = false; // 禁用周围摄像机
            backCamera.gameObject.SetActive (true); // 启用后置摄像机游戏对象
            aroundCamera.gameObject.SetActive (false); // 禁用周围摄像机游戏对象
            currentCamera = backCamera; // 当前摄像机为后置摄像机
            
            //////////////////// 用于后置摄像机的代码
            backCamera.fieldOfView = backCamera.fieldOfView + outsideControls.Vertical * 20f * Time.deltaTime; // 根据控制变量调整后置摄像机的视野
            if (backCamera.fieldOfView > 85) {
                backCamera.fieldOfView = 85;
            }
            if (backCamera.fieldOfView < 50) {
                backCamera.fieldOfView = 50;
            }
            if (backCamera.fieldOfView < 60) {
                backCamera.fieldOfView = backCamera.fieldOfView += 10f * Time.deltaTime;
            }
            if (backCamera.fieldOfView > 60) {
                backCamera.fieldOfView = backCamera.fieldOfView -= 10f * Time.deltaTime;
            }
            
            float wantedRotationAngle = cameraTarget.eulerAngles.y; // 目标旋转角度为摄像机目标的Y轴旋转角度
            float wantedHeight = cameraTarget.position.y + height; // 目标高度为摄像机目标的Y轴位置加上高度
            float currentRotationAngle = currentCamera.transform.eulerAngles.y; // 当前旋转角度为当前摄像机的Y轴旋转角度
            float currentHeight = currentCamera.transform.position.y; // 当前高度为当前摄像机的Y轴位置
            
            currentRotationAngle = Mathf.LerpAngle (currentRotationAngle, wantedRotationAngle, 3 * Time.deltaTime); // 平滑旋转角度
            currentHeight = Mathf.Lerp (currentHeight, wantedHeight, 2 * Time.deltaTime); // 平滑高度
            
            Quaternion currentRotation = Quaternion.Euler (0, currentRotationAngle, 0); // 当前旋转角度
            currentCamera.transform.position = cameraTarget.position; // 设置摄像机的位置为摄像机目标的位置
            currentCamera.transform.position -= currentRotation * Vector3.forward * dist; // 根据旋转角度和距离调整摄像机的位置
            currentCamera.transform.position = new Vector3 (currentCamera.transform.position.x, currentHeight, currentCamera.transform.position.z); // 调整摄像机的高度
            currentCamera.transform.LookAt (cameraTarget); // 朝向摄像机目标

            // 新的摄像机特性。
            // 现在摄像机会随着骑行者的倾斜而倾斜，所以地平线不总是水平的 :)
            // 如果不喜欢，可以禁用
            // 从这里开始 -----------------------------------------------------------------------

            // 根据自行车的倾斜旋转摄像机
            if (cameraTarget.transform.eulerAngles.z >0 && cameraTarget.transform.eulerAngles.z < 180) {
                currentTargetAngle = cameraTarget.transform.eulerAngles.z/10;
            }
            if (cameraTarget.transform.eulerAngles.z >180){
                currentTargetAngle = -(360-cameraTarget.transform.eulerAngles.z)/10;
            }
            currentCamera.transform.rotation = Quaternion.Euler (height*10, currentRotationAngle, currentTargetAngle);
            // 到这里结束 -------------------------------------------------------------------------
        #if UNITY_STANDALONE || UNITY_WEBPLAYER // 仅为移动设备启用摄像机旋转，以便在任意位置进行触摸
        }
        #endif
    }
}