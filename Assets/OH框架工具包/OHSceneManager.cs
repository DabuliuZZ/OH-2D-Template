using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OHTools;
using Sirenix.OdinInspector;
using DG.Tweening;

/// <summary>
/// 场景跳转管理器，单例模式
/// </summary>
public class OHSceneManager : OHMonoSingleton<OHSceneManager>
{
    [Header("黑屏组件")]
    [SerializeField, LabelText("黑屏图像")]
    private Image _blackScreen;

    [SerializeField, LabelText("黑屏淡入淡出时长")]
    private float _fadeDuration = 1f;

    [Header("音频淡出")]
    [SerializeField, LabelText("需要淡出的音频播放器列表")]
    private AudioSource[] _audioPlayersToFade;

    private string _targetSceneName;
    private Tween _fadeTween;

    void Start()
    {
        // 启动时黑屏淡出
        FadeOut();
    }

    /// <summary>
    /// 黑屏淡出（从黑到透明）
    /// </summary>
    private void FadeOut()
    {
        _fadeTween?.Kill();
        
        // 设置初始为全黑
        if (_blackScreen != null)
        {
            _blackScreen.gameObject.SetActive(true);
            
            // 淡出到透明
            _fadeTween = _blackScreen.DOFade(0f, _fadeDuration).SetEase(Ease.OutQuad);
        }
    }

    
    /// <summary>
    /// 淡出所有音频播放器音量至0
    /// </summary>
    private void FadeOutAudioPlayers()
    {
        if (_audioPlayersToFade == null || _audioPlayersToFade.Length == 0)
            return;

        foreach (var audioPlayer in _audioPlayersToFade)
        {
            if (audioPlayer != null && audioPlayer.isPlaying)
            {
                audioPlayer.DOFade(0f, _fadeDuration).SetEase(Ease.InQuad);
            }
        }
    }

    /// <summary>
    /// 跳转到指定场景（黑屏渐入后跳转）
    /// </summary>
    /// <param name="sceneName">目标场景名，为空时使用Inspector中配置的场景名</param>
    [Button("跳转场景")]
    public void LoadScene(string sceneName = null)
    {
        // 使用传入的场景名或默认场景名
        string targetScene = string.IsNullOrEmpty(sceneName) ? _targetSceneName : sceneName;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[OHSceneManager] 目标场景名为空，无法跳转");
            return;
        }

        // 停止之前的淡出动画
        _fadeTween?.Kill();

        // 黑屏渐入
        if (_blackScreen != null)
        {
            _blackScreen.DOFade(1f, _fadeDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                // 渐入完成后跳转场景
                SceneManager.LoadScene(targetScene);
            });

            // 同时淡出所有音频播放器音量
            FadeOutAudioPlayers();
        }
        else
        {
            // 如果没有黑屏图像，直接跳转
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }

    /// <summary>
    /// 清理动画
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _fadeTween?.Kill();
    }
}