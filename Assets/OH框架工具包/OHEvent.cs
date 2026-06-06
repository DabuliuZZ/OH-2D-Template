
namespace OHTools
{
    // 定义事件名称的枚举
    public enum OHEvent
    {
        SceneLoadByName,       // 按场景名跳转场景
        SceneLoadNext,         // 跳转到下一个场景（Build Settings 中的下一个序号）
        EyeClose,              // 闭眼表现
        EyeOpen,               // 睁眼表现
        EyeSetValue,           // 设置眼皮张开值（float）
        PlayAudio,             // 播放一次音效（AudioClip）
        PlayAudioByName,       // 按名称播放一次音效（string）
        PlayBGM,               // 播放循环背景音乐（AudioClip）
        PlayBGMByName,         // 按名称播放循环背景音乐（string）
        StopAudio,             // 停止指定音效（AudioClip）
        StopAudioByName,       // 按名称停止指定音效（string）
    }
}
