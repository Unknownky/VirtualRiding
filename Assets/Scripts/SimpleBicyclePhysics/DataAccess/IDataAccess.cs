using UnityEngine;

/// <summary>
/// 数据获取接口
/// </summary>
public interface IDataAccess
{
    void SimulateDataUpdate()
    {
        //更新相关参数
        UpdateHorizontalAndAngle();
        UpdateCurrentAngularVelocity();//更新角速度
        UpdateAccelerate();//更新加速度
    }

    void UpdateHorizontalAndAngle();

    void UpdateCurrentAngularVelocity();

    void UpdateAccelerate();
}
