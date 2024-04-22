using UnityEngine;
using System.Collections;

public class skidMarksDestroy : MonoBehaviour
{

    public int destoryThisTime = 3;//以秒为单位的销毁时间
    public float alphaFade = 0.001F;//抽象的值，-0.001表示大约20秒内淡出

    private Color color = Color.black;

    Material skidMat;

    // 初始化
    void Start()
    {

        skidMat = GetComponentInChildren<MeshRenderer>().material;
        Destroy(gameObject, destoryThisTime);
    }

    // 每帧更新
    void Update()
    {
        // 将刹车痕迹的透明度进行淡出效果
        Color newColor = skidMat.color;
        newColor.a -= alphaFade;
        skidMat.color = newColor;

    }
}
