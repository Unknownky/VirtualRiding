using UnityEngine;
using System.Collections;

/// <summary>
/// 该脚本模拟踏板旋转和特技，骑手踩踏板时的左右小倾斜
/// </summary>
public class pedalControls : MonoBehaviour
{

    private bicycle_code linkToBike;// 创建与对应自行车脚本的链接
    private biker_logic_mecanim linkToRider;// 创建与骑手脚本的链接

    public GameObject pedalLeft;
    public GameObject pedalRight;

    public string characterName;

    private GameObject ctrlHub;// 带有控制变量脚本的游戏对象
    private controlHub outsideControls;// 创建与对应自行车脚本的链接

    private float energy = 0;// 踩踏能量，在加速关闭后释放

    // 用于将CoM左右移动以模拟踏板旋转
    public Transform CoM; // CoM对象

    // 用于移动骑手的骨盆
    public Transform veloMan;

    // 特技自行车
    public Transform stuntBike;

    // 特殊的临时状态“在特技中”，用于防止超过最大角度后摔倒
    public bool stuntIsOn = false;

    // 在特技中的临时状态
    private bool inStunt = false;
    void Start()
    {

        ctrlHub = GameObject.Find("gameScenario");// 查找名为“gameScenario”的游戏对象
        outsideControls = ctrlHub.GetComponent<controlHub>();// 将C#移动控制脚本连接到此脚本

        linkToBike = GameObject.Find("rigid_bike").GetComponent<bicycle_code>();
        linkToRider = GameObject.Find("pico_chan_chr").GetComponent<biker_logic_mecanim>();

    }

    void Update()
    {
     //按下“空格”键开始兔子跳特技
        if (Input.GetKeyDown("space"))
        {
            StartCoroutine(StuntBunnyHope());
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            StartCoroutine(StuntBackFlip360());
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(StuntTurnLeft180());
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            StartCoroutine(StuntBunnyShiftRight());
        }

        if (Input.GetKeyDown(KeyCode.Slash))
        {
            StartCoroutine(StuntHoldForOneSecond());
        }
        //"2" for manual
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StuntManual();
        }

    }

    void FixedUpdate()
    {
        //pedals rotation part
        if (outsideControls.Vertical > 0)
        {
            this.transform.rotation = this.transform.rotation * Quaternion.Euler(linkToBike.bikeSpeed / 4, 0, 0);
            pedalRight.transform.rotation = pedalRight.transform.rotation * Quaternion.Euler(-linkToBike.bikeSpeed / -4, 0, 0);
            pedalLeft.transform.rotation = pedalLeft.transform.rotation * Quaternion.Euler(-linkToBike.bikeSpeed / 4, 0, 0);
            if (energy < 10)
            {
                energy = energy + 0.01f;
            }

            if (Mathf.Abs(CoM.transform.localPosition.x) < 0.1f)
            {
                var tmpRidPlvs01 = veloMan.transform.localEulerAngles;
                tmpRidPlvs01.z = CoM.transform.localPosition.x * 200;
                veloMan.transform.localEulerAngles = tmpRidPlvs01;
                //(sometimes looks strange on bicycles with high seat. So, you might just disable it when needed)
            }
            var tmpCoM01 = CoM.transform.localPosition;
            tmpCoM01.x = -0.02f + (Mathf.Abs(this.transform.localRotation.x) / 25);//leaning bicycle when pedaling
            CoM.transform.localPosition = tmpCoM01;

        }
        else EnergyWaste();//need to move pedals some time after stop acceleration

        //movement body of rider's pelvis when cornering(sometimes looks strange on bicycles with high seat. So, you might just disable it when needed)
        var tmpRidPlvs02 = veloMan.transform.localPosition;
        tmpRidPlvs02.x = outsideControls.Horizontal / 10;
        veloMan.transform.localPosition = tmpRidPlvs02;



    }

    //function when player stop accelerating and rider still slowly rotating pedals
    void EnergyWaste()
    {
        if (energy > 0)
        {
            var tmpEnergy = 10 - energy;
            this.transform.rotation = this.transform.rotation * Quaternion.Euler((linkToBike.bikeSpeed - tmpEnergy) / 4, 0, 0);
            pedalRight.transform.rotation = pedalRight.transform.rotation * Quaternion.Euler(-(linkToBike.bikeSpeed - tmpEnergy) / -4, 0, 0);
            pedalLeft.transform.rotation = pedalLeft.transform.rotation * Quaternion.Euler(-(linkToBike.bikeSpeed - tmpEnergy) / 4, 0, 0);
            energy = energy - 0.1f;

        }
    }

    //trick to do not crash for one second. You need that for riding ramps
    IEnumerator StuntHoldForOneSecond()
    {
        stuntIsOn = true;
        yield return new WaitForSeconds(0.5f);//1 second seems too long :) now it's half of second 0.5ff. Make 1 for actually 1 second
        if (!inStunt)
        {
            stuntIsOn = false;
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // applying physical forces to immitate stunts///////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //void StuntBunnyHope (){
    IEnumerator StuntBunnyHope()
    {
        linkToRider.PlayA("bannyhope");//animation is optional. You may delete this string with no bad aftermath
        stuntBike.GetComponent<Rigidbody>().AddForce(Vector3.up * 40000);//push bike up
        yield return new WaitForSeconds(0.1f);//a little pause between applying force
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -14000);//pull front wheel(turn bike around CoM)
        yield return new WaitForSeconds(0.2f);//a little pause between applying force
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * 20000);//push front down and pull rear up
    }
    void StuntManual()
    {
        linkToRider.PlayA("manual");
    }

    //here is stunts
    IEnumerator StuntBackFlip360()
    {
        linkToRider.PlayA("backflip360");
        stuntIsOn = true;
        inStunt = true;
        //CoM.transform.localPosition.y = 0;
        var tmpCoM01 = CoM.transform.localPosition;
        tmpCoM01.y = 0;
        CoM.transform.localPosition = tmpCoM01;
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddTorque(transform.right * -2500000);
        yield return new WaitForSeconds(0.7f);
        inStunt = false;
        stuntIsOn = false;
    }

    IEnumerator StuntTurnLeft180()
    {
        linkToRider.PlayA("rightflip180");
        stuntIsOn = true;
        inStunt = true;
        var tmpCoM02 = CoM.transform.localPosition;
        tmpCoM02.y = 0;
        CoM.transform.localPosition = tmpCoM02;
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * 10000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * 10000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * 10000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * 10000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * 10000);
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * 10000);
        yield return new WaitForSeconds(0.7f);
        inStunt = false;
        stuntIsOn = false;
    }

    IEnumerator StuntBunnyShiftRight()
    {
        linkToRider.PlayA("bannyhope");

        stuntBike.GetComponent<Rigidbody>().AddForce(Vector3.up * 45000);//push bike up
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.right * -4000);//pull front wheel(turn bike around CoM)
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * 1000);//turn bike right
        yield return new WaitForSeconds(0.1f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeForce(Vector3.right * 24000);//push bike right
        yield return new WaitForSeconds(0.2f);
        stuntBike.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * -3000);//turn bike left


    }
}