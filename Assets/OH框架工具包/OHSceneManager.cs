using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OHTools;
using Sirenix.OdinInspector;
using DG.Tweening;

/// <summary>
/// 场景跳转过渡模式
/// </summary>
public enum TransitionMode
{
    [LabelText("黑屏淡入淡出")] BlackScreen,
    [LabelText("眼皮闭合")] EyeBlink,
}

/// <summary>
/// 场景跳转管理器，全局持久单例
/// </summary>
public class OHSceneManager : OHMonoSingleton<OHSceneManager>
{
    [Header("过渡模式")]
    [SerializeField, LabelText("过渡模式")]
    private TransitionMode _transitionMode = TransitionMode.BlackScreen;

    [Header("黑屏组件")]
    [SerializeField, LabelText("黑屏图像")]
    private Image _blackScreen;

    [SerializeField, LabelText("黑屏淡入淡出时长")]
    private float _fadeDuration = 1f;

    [Header("眼皮组件")]
    [SerializeField, LabelText("眼皮遮罩图像")]
    private Image _eyeBlinkOverlay;

    [SerializeField, LabelText("眼皮颜色")]
    private Color _eyeBlinkColor = Color.black;

    [SerializeField, LabelText("眼皮睁闭动画时长")]
    private float _eyeBlinkDuration = 0.6f;

    [Header("音频淡出")]
    [SerializeField, LabelText("自动收集场景中所有音频播放器")]
    private bool _autoFindAudioPlayers;

    [SerializeField, LabelText("需要淡出的音频播放器列表")]
    private AudioSource[] _audioPlayersToFade;

    private Tween _transitionTween;
    private Material _eyeMaterial;

    void Start()
    {
        // 初始化眼皮材质
        if (_eyeBlinkOverlay != null)
        {
            _eyeMaterial = _eyeBlinkOverlay.material;
            _eyeMaterial.SetColor("_Color", _eyeBlinkColor);
        }

        // 启动时播放睁眼/淡出动画
        PlayOpenAnimation();
    }

    void OnEnable()
    {
        OHEventCenter.AddEventListener<string>(OHEvent.SceneLoadByName, LoadScene);
        OHEventCenter.AddEventListener(OHEvent.SceneLoadNext, LoadNextScene);
        OHEventCenter.AddEventListener<System.Action>(OHEvent.EyeClose, OnEyeCloseEvent);
        OHEventCenter.AddEventListener<System.Action>(OHEvent.EyeOpen, OnEyeOpenEvent);
        OHEventCenter.AddEventListener<float>(OHEvent.EyeSetValue, OnEyeSetValueEvent);
        // 监听场景加载完成，播放睁眼动画
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        OHEventCenter.RemoveEventListener<string>(OHEvent.SceneLoadByName, LoadScene);
        OHEventCenter.RemoveEventListener(OHEvent.SceneLoadNext, LoadNextScene);
        OHEventCenter.RemoveEventListener<System.Action>(OHEvent.EyeClose, OnEyeCloseEvent);
        OHEventCenter.RemoveEventListener<System.Action>(OHEvent.EyeOpen, OnEyeOpenEvent);
        OHEventCenter.RemoveEventListener<float>(OHEvent.EyeSetValue, OnEyeSetValueEvent);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成回调，播放睁眼动画
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayOpenAnimation();
    }

    #region 公共跳转方法

    /// <summary>
    /// 跳转到指定场景
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    [Button("跳转场景")]
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[OHSceneManager] 目标场景名为空，无法跳转");
            return;
        }

        ExecuteSceneTransition(sceneName);
    }

    /// <summary>
    /// 跳转到 Build Settings 中的下一个场景
    /// </summary>
    [Button("跳转下一个场景")]
    public void LoadNextScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("[OHSceneManager] 已是最后一个场景，无法跳转下一个");
            return;
        }

        string nextSceneName = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(nextSceneName);

        ExecuteSceneTransition(sceneName);
    }

    #endregion

    #region 过渡动画

    /// <summary>
    /// 执行场景跳转（根据当前模式选择闭眼/黑屏动画）
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    private void ExecuteSceneTransition(string sceneName)
    {
        _transitionTween?.Kill();

        switch (_transitionMode)
        {
            case TransitionMode.EyeBlink:
                EyeCloseAnimation(sceneName);
                break;
            default:
                BlackScreenFadeIn(sceneName);
                break;
        }

        FadeOutAudioPlayers();
    }

    /// <summary>
    /// 播放开场动画（淡出黑屏 或 睁眼）
    /// </summary>
    private void PlayOpenAnimation()
    {
        _transitionTween?.Kill();

        switch (_transitionMode)
        {
            case TransitionMode.EyeBlink:
                EyeOpenAnimation();
                break;
            default:
                BlackScreenFadeOut();
                break;
        }
    }

    #endregion

    #region 黑屏模式

    /// <summary>
    /// 黑屏淡入（从透明到黑）
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    private void BlackScreenFadeIn(string sceneName)
    {
        if (_blackScreen != null)
        {
            _blackScreen.DOFade(1f, _fadeDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                LoadSceneAsync(sceneName).Forget();
            });
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// 黑屏淡出（从黑到透明）
    /// </summary>
    private void BlackScreenFadeOut()
    {
        if (_blackScreen == null) return;

        _blackScreen.gameObject.SetActive(true);
        // 确保初始为全黑
        var color = _blackScreen.color;
        color.a = 1f;
        _blackScreen.color = color;

        _transitionTween = _blackScreen.DOFade(0f, _fadeDuration).SetEase(Ease.OutQuad);
    }

    #endregion

    #region 眼皮模式

    /// <summary>
    /// 闭眼事件处理
    /// </summary>
    private void OnEyeCloseEvent(System.Action onComplete)
    {
        PlayEyeClose(onComplete);
    }

    /// <summary>
    /// 睁眼事件处理
    /// </summary>
    private void OnEyeOpenEvent(System.Action onComplete)
    {
        PlayEyeOpen(onComplete);
    }

    /// <summary>
    /// 设置眼皮值事件处理
    /// </summary>
    private void OnEyeSetValueEvent(float value)
    {
        SetEyeOpen(value);
    }

    /// <summary>
    /// 设置眼皮张开程度（0=闭合，1=完全睁开）
    /// </summary>
    /// <param name="value">眼皮张开值</param>
    public void SetEyeOpen(float value)
    {
        if (_eyeMaterial != null)
            _eyeMaterial.SetFloat("_EyeOpen", value);
    }

    /// <summary>
    /// 纯表现：播放闭眼动画，不触发场景跳转
    /// </summary>
    /// <param name="onComplete">动画完成回调（可选）</param>
    public void PlayEyeClose(System.Action onComplete = null)
    {
        _transitionTween?.Kill();

        if (_eyeMaterial == null) return;

        _transitionTween = DOVirtual.Float(1f, 0f, _eyeBlinkDuration, value =>
        {
            _eyeMaterial.SetFloat("_EyeOpen", value);
        }).SetEase(Ease.InQuad).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 纯表现：播放睁眼动画，不触发场景跳转
    /// </summary>
    /// <param name="onComplete">动画完成回调（可选）</param>
    public void PlayEyeOpen(System.Action onComplete = null)
    {
        _transitionTween?.Kill();

        if (_eyeMaterial == null) return;

        _eyeMaterial.SetFloat("_EyeOpen", 0f);

        _transitionTween = DOVirtual.Float(0f, 1f, _eyeBlinkDuration, value =>
        {
            _eyeMaterial.SetFloat("_EyeOpen", value);
        }).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 闭眼动画（从睁眼到闭合）完成后加载场景
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    private void EyeCloseAnimation(string sceneName)
    {
        if (_eyeMaterial == null)
        {
            Debug.LogWarning("[OHSceneManager] 眼皮材质未配置，直接跳转");
            SceneManager.LoadScene(sceneName);
            return;
        }

        _transitionTween = DOVirtual.Float(1f, 0f, _eyeBlinkDuration, value =>
        {
            _eyeMaterial.SetFloat("_EyeOpen", value);
        }).SetEase(Ease.InQuad).OnComplete(() =>
        {
            LoadSceneAsync(sceneName).Forget();
        });
    }

    /// <summary>
    /// 睁眼动画（从闭合到睁开）
    /// </summary>
    private void EyeOpenAnimation()
    {
        if (_eyeMaterial == null) return;

        // 确保从闭合状态开始
        _eyeMaterial.SetFloat("_EyeOpen", 0f);

        _transitionTween = DOVirtual.Float(0f, 1f, _eyeBlinkDuration, value =>
        {
            _eyeMaterial.SetFloat("_EyeOpen", value);
        }).SetEase(Ease.OutQuad);
    }

    #endregion

    #region 音频淡出

    /// <summary>
    /// 淡出所有音频播放器音量至0
    /// </summary>
    private void FadeOutAudioPlayers()
    {
        float duration = _transitionMode == TransitionMode.EyeBlink ? _eyeBlinkDuration : _fadeDuration;

        if (_autoFindAudioPlayers)
            _audioPlayersToFade = FindObjectsOfType<AudioSource>();

        if (_audioPlayersToFade == null || _audioPlayersToFade.Length == 0) return;

        foreach (var audioPlayer in _audioPlayersToFade)
        {
            if (audioPlayer != null && audioPlayer.isPlaying)
            {
                audioPlayer.DOFade(0f, duration).SetEase(Ease.InQuad);
            }
        }
    }

    #endregion

    #region 异步加载

    /// <summary>
    /// 异步加载场景，等待加载完成后再激活
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    private async UniTaskVoid LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            await UniTask.Yield();
        }

        asyncLoad.allowSceneActivation = true;
    }

    #endregion

    /// <summary>
    /// 清理动画和材质
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _transitionTween?.Kill();

        // 销毁动态创建的眼皮材质实例，避免内存泄漏
        if (_eyeBlinkOverlay != null && _eyeMaterial != null)
        {
            Destroy(_eyeMaterial);
        }
    }
}
