using UnityEngine;
using System.Collections;

public class keyboardControls : MonoBehaviour {

    private GameObject ctrlHub;// 与对应自行车脚本的链接
    private controlHub outsideControls;// 与对应自行车脚本的链接
    

    // Use this for initialization
    void Start () {
        ctrlHub = GameObject.Find("gameScenario");// 链接到名为"controlHub"的GameObject上的脚本
        outsideControls = ctrlHub.GetComponent<controlHub>();// 与对应自行车脚本的链接
    }
    
    // Update is called once per frame
    void Update () {
        //////////////////////////////////// 加速、刹车和"全油门-手动技巧" //////////////////////////////////////////////
        // Alpha2是键盘上的"2"键。用于进行手动技巧。也可以通过100%的"移动摇杆油门"来实现
        if (!Input.GetKey (KeyCode.Alpha2)) {
            outsideControls.Vertical = Input.GetAxis ("Vertical") / 1.112f;// 为了得到小于0.9的加速度，以防止后轮离地（后轮离地在大于0.9时开始）
            if(Input.GetAxis ("Vertical") <0) outsideControls.Vertical = outsideControls.Vertical * 1.112f;// 需要得到1（全力）来刹前刹车
        }

        //////////////////////////////////// 转向 /////////////////////////////////////////////////////////////////////////
        outsideControls.Horizontal = Input.GetAxis("Horizontal");
        if (Input.GetKey (KeyCode.Alpha2)) outsideControls.Vertical = 1;
        //}

        //////////////////////////////////// 骑手质量位移 ////////////////////////////////////////////////////////////
        // 这些代码控制骑手在自行车上的质量位移（垂直方向）
        if (Input.GetKey (KeyCode.F)) {
            outsideControls.VerticalMassShift = outsideControls.VerticalMassShift += 0.1f;
            if (outsideControls.VerticalMassShift > 1.0f) outsideControls.VerticalMassShift = 1.0f;
        }

        if (Input.GetKey(KeyCode.V)){
            outsideControls.VerticalMassShift = outsideControls.VerticalMassShift -= 0.1f;
            if (outsideControls.VerticalMassShift < -1.0f) outsideControls.VerticalMassShift = -1.0f;
        }
        if(!Input.GetKey(KeyCode.F) && !Input.GetKey(KeyCode.V)) outsideControls.VerticalMassShift = 0;

        // 这些代码控制骑手在自行车上的质量位移（水平方向）
        if (Input.GetKey(KeyCode.E)){
            outsideControls.HorizontalMassShift = outsideControls.HorizontalMassShift += 0.1f;
            if (outsideControls.HorizontalMassShift >1.0f) outsideControls.HorizontalMassShift = 1.0f;
        }

        if (Input.GetKey(KeyCode.Q)){
            outsideControls.HorizontalMassShift = outsideControls.HorizontalMassShift -= 0.1f;
            if (outsideControls.HorizontalMassShift < -1.0f) outsideControls.HorizontalMassShift = -1.0f;
        }
        if(!Input.GetKey(KeyCode.E) && !Input.GetKey(KeyCode.Q)) outsideControls.HorizontalMassShift = 0;


        //////////////////////////////////// 后刹车 ////////////////////////////////////////////////////////////////
        // 后刹车
        if (Input.GetKey (KeyCode.X)) {
            outsideControls.rearBrakeOn = true;
        } else
            outsideControls.rearBrakeOn = false;

        //////////////////////////////////// 重新开始 ////////////////////////////////////////////////////////////////
        // 重新开始和完全重新开始
        if (Input.GetKey (KeyCode.R)) {
            outsideControls.restartBike = true;
        } else
            outsideControls.restartBike = false;

        // 按下右Shift键进行完全重新开始
        if (Input.GetKey (KeyCode.RightShift)) {
            outsideControls.fullRestartBike = true;
        } else
            outsideControls.fullRestartBike = false;

        //////////////////////////////////// 倒车 ////////////////////////////////////////////////////////////////
        // 倒车
        if(Input.GetKeyDown(KeyCode.C)){
                outsideControls.reverse = true;
        } else outsideControls.reverse = false;
        ///
    }
}
