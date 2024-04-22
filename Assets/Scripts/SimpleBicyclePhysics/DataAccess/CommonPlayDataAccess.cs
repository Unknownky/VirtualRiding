using UnityEngine;
/// <summary>
/// 当前类继承自数据获取方式基类，适配游玩数据的获取
/// </summary>
public class CommonPlayDataAccess : MonoBehaviour, IDataAccess
{
    private float minAngleX = -45f;
    private float maxAngleX = 45f;
    public void UpdateAccelerate()
    {
        DataServer target = DataServer.GetInstance();
        target.vertical = Input.GetAxis("Vertical");
#if UNITY_EDITOR
        Debug.Log($"当前获取竖直值为：{Input.GetAxis("Vertical")}当前获取水平值为{target.vertical}");
#endif
        target.accelerate = target.defaultAccelerate * target.vertical;
#if UNITY_EDITOR
        Debug.Log($"commonplaydataaccess中的accelerate为{target.defaultAccelerate}");
#endif
    }

    public void UpdateCurrentAngularVelocity()
    {
        DataServer target = DataServer.GetInstance();
        target.angularVelocityPercent = target.angle / target.angularVelocityMax;//更新角速度百分比
        target.currentAngularVelocity = target.angularVelocityMax * target.angularVelocityPercent;//更新角速度参数
    }

    public void UpdateHorizontalAndAngle()
    {
        DataServer target = DataServer.GetInstance();
        target.horizontal = Input.GetAxis("Horizontal");
        if (target.horizontal == 0)//不操作时自动回正车头
        {
            target.angle = 0;
            return;
        }
        target.angle += target.turningHeadSpeed * target.horizontal * Time.deltaTime;//左为负，右为正，更新当前车头角度
        target.angle = Mathf.Clamp(target.angle, minAngleX, maxAngleX);//限制angle的值
    }
}
