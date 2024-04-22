using UnityEngine;

/// <summary>
/// 使用当前类进行运行方式的持久化
/// </summary>
[CreateAssetMenu(menuName = ("MoveControllerWay/NewMoveControllerWay"), fileName = ("NewMoveControllerWay"))]
public class ControllerContainer : ScriptableObject
{
    [SerializeField]public Object moveControllerWay;
    [SerializeField] public string wayDescription;
}
