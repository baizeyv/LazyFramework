using Lazy;

namespace Lazy
{
    public enum TimeRequestStatus
    {
        Unrequested, // # 未请求
        Success, // # 请求成功
        Fail, // # 请求失败
        Requesting // # 正在请求中
        ,
    }
}
