
namespace OHTools
{
    // 定义事件名称的枚举
    public enum OHEvent
    {
        SceneLoadByName,       // 按场景名跳转场景
        SceneLoadNext,         // 跳转到下一个场景（Build Settings 中的下一个序号）
        EyeClose,              // 闭眼表现
        EyeOpen,               // 睁眼表现
        EyeSetValue,           // 设置眼皮张开值（float 参数）
    }
}
