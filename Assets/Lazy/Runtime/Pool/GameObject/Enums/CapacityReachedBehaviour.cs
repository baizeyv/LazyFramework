namespace Lazy
{
    /// <summary>
    /// * 容量到达极限的行为
    /// </summary>
    public enum CapacityReachedBehaviour
    {
        ReturnNullableClone, // # 返回空克隆
        Instantiate, // # 实例化
        InstantiateWithCallback, // # 实例化及回调
        Recycle, // # 回收
        ThrowException // # 抛异常
        ,
    }
}
