using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace OHTools
{
    /// <summary>
    /// 音频播放器，继承自 OHMonoSingleton 以支持多种单例模式。
    /// 支持两种触发方式：直接调用单例方法 或 通过 OHEventCenter 发送事件触发。
    /// </summary>
    public class OHAudioPlayer : OHMonoSingleton<OHAudioPlayer>
    {
        // 池中可复用的 AudioSource 实例
        private ObjectPool<AudioSource> pool;

        // 跟踪每个 clip 正在使用的 AudioSource
        private readonly Dictionary<AudioClip, List<AudioSource>> activePlayers =
            new Dictionary<AudioClip, List<AudioSource>>();

        [Header("内置音频列表")]
        [SerializeField, LabelText("音频列表"), Tooltip("可通过 clip 文件名播放的音频列表")]
        private List<AudioClip> _audioClips = new List<AudioClip>();

        [Header("事件监听")]
        [SerializeField, LabelText("监听音频事件"), Tooltip("开启后将通过事件中心监听音频播放事件")]
        private bool _listenAudioEvents = true;

        protected override void Awake()
        {
            base.Awake();

            pool = new ObjectPool<AudioSource>(
                createFunc: CreateNewAudioSource,
                actionOnGet: src => src.gameObject.SetActive(true),
                actionOnRelease: ResetAudioSource,
                actionOnDestroy: src => Destroy(src.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 100
            );
        }

        /// <summary>
        /// 根据 clip 文件名从内置列表中查找 AudioClip。
        /// </summary>
        public AudioClip FindClipByName(string clipName)
        {
            return _audioClips.Find(c => c != null && c.name == clipName);
        }

        private void OnEnable()
        {
            if (_listenAudioEvents)
            {
                OHEventCenter.AddEventListener<AudioClip, float>(OHEvent.PlayAudio, OnPlayAudioEvent);
                OHEventCenter.AddEventListener<AudioClip, float>(OHEvent.PlayBGM, OnPlayBGMEvent);
                OHEventCenter.AddEventListener<string, float>(OHEvent.PlayAudioByName, OnPlayAudioByNameEvent);
                OHEventCenter.AddEventListener<string, float>(OHEvent.PlayBGMByName, OnPlayBGMByNameEvent);
                OHEventCenter.AddEventListener<AudioClip>(OHEvent.StopAudio, OnStopAudioEvent);
                OHEventCenter.AddEventListener<string>(OHEvent.StopAudioByName, OnStopAudioByNameEvent);
            }
        }

        private void OnDisable()
        {
            OHEventCenter.RemoveEventListener<AudioClip, float>(OHEvent.PlayAudio, OnPlayAudioEvent);
            OHEventCenter.RemoveEventListener<AudioClip, float>(OHEvent.PlayBGM, OnPlayBGMEvent);
            OHEventCenter.RemoveEventListener<string, float>(OHEvent.PlayAudioByName, OnPlayAudioByNameEvent);
            OHEventCenter.RemoveEventListener<string, float>(OHEvent.PlayBGMByName, OnPlayBGMByNameEvent);
            OHEventCenter.RemoveEventListener<AudioClip>(OHEvent.StopAudio, OnStopAudioEvent);
            OHEventCenter.RemoveEventListener<string>(OHEvent.StopAudioByName, OnStopAudioByNameEvent);
        }

        protected override void OnDestroy()
        {
            OHEventCenter.RemoveEventListener<AudioClip, float>(OHEvent.PlayAudio, OnPlayAudioEvent);
            OHEventCenter.RemoveEventListener<AudioClip, float>(OHEvent.PlayBGM, OnPlayBGMEvent);
            OHEventCenter.RemoveEventListener<string, float>(OHEvent.PlayAudioByName, OnPlayAudioByNameEvent);
            OHEventCenter.RemoveEventListener<string, float>(OHEvent.PlayBGMByName, OnPlayBGMByNameEvent);
            OHEventCenter.RemoveEventListener<AudioClip>(OHEvent.StopAudio, OnStopAudioEvent);
            OHEventCenter.RemoveEventListener<string>(OHEvent.StopAudioByName, OnStopAudioByNameEvent);
            
            base.OnDestroy();
        }

        #region 事件回调

        /// <summary>
        /// 接收 PlayAudio 事件的回调
        /// </summary>
        private void OnPlayAudioEvent(AudioClip clip, float volume)
        {
            PlayAudio(clip, volume);
        }

        /// <summary>
        /// 接收 PlayBGM 事件的回调
        /// </summary>
        private void OnPlayBGMEvent(AudioClip clip, float volume)
        {
            PlayBGM(clip, volume);
        }

        /// <summary>
        /// 接收 PlayAudioByName 事件的回调，根据 clip 文件名查找并播放音效。
        /// </summary>
        private void OnPlayAudioByNameEvent(string clipName, float volume)
        {
            PlayAudioByName(clipName, volume);
        }

        /// <summary>
        /// 接收 PlayBGMByName 事件的回调，根据 clip 文件名查找并播放 BGM。
        /// </summary>
        private void OnPlayBGMByNameEvent(string clipName, float volume)
        {
            PlayBGMByName(clipName, volume);
        }

        /// <summary>
        /// 接收 StopAudio 事件的回调
        /// </summary>
        private void OnStopAudioEvent(AudioClip clip)
        {
            StopPlayerWithClip(clip);
        }

        /// <summary>
        /// 接收 StopAudioByName 事件的回调，根据名称停止音效。
        /// </summary>
        private void OnStopAudioByNameEvent(string clipName)
        {
            StopPlayerWithClipByName(clipName);
        }

        #endregion

        /// <summary>
        /// 创建一个新的子级 GameObject，只添加 AudioSource，用于播放音频。
        /// </summary>
        private AudioSource CreateNewAudioSource()
        {
            var go = new GameObject("AudioPlayer");
            go.transform.SetParent(transform, worldPositionStays: false);
            return go.AddComponent<AudioSource>();
        }

        /// <summary>
        /// 停止给定的 AudioSource，并将其释放入池。
        /// </summary>
        private void ReleaseAudioSource(AudioSource src)
        {
            if (src == null) return;

            var clip = src.clip;
            if (clip != null && activePlayers.TryGetValue(clip, out var list))
            {
                list.Remove(src);
                if (list.Count == 0)
                    activePlayers.Remove(clip);
            }

            pool.Release(src);
        }

        /// <summary>
        /// 重置并禁用释放入池的 AudioSource。
        /// </summary>
        private void ResetAudioSource(AudioSource src)
        {
            src.Stop();
            src.clip = null;
            src.loop = false;
            src.volume = 1;
            src.gameObject.SetActive(false);
        }

        #region 公共接口

        /// <summary>
        /// 播放一次指定 clip，结束后自动回收。
        /// </summary>
        public void PlayAudio(AudioClip clip, float volume = 1f)
        {
            Play(clip, false, volume);
        }

        /// <summary>
        /// 根据 clip 文件名播放一次音效，从内置音频列表中查找。
        /// </summary>
        public void PlayAudioByName(string clipName, float volume = 1f)
        {
            var clip = FindClipByName(clipName);
            if (clip != null) Play(clip, false, volume);
        }

        /// <summary>
        /// 播放指定 clip 并循环。
        /// </summary>
        public void PlayBGM(AudioClip clip, float volume = 1f)
        {
            Play(clip, true, volume);
        }

        /// <summary>
        /// 根据 clip 文件名播放循环背景音乐，从内置音频列表中查找。
        /// </summary>
        public void PlayBGMByName(string clipName, float volume = 1f)
        {
            var clip = FindClipByName(clipName);
            if (clip != null) Play(clip, true, volume);
        }

        /// <summary>
        /// 停止并回收指定 clip 的一个 AudioSource 实例。
        /// </summary>
        public void StopPlayerWithClip(AudioClip clip)
        {
            ReleaseAudioSource(GetPlayerWithClip(clip));
        }

        /// <summary>
        /// 根据 clip 文件名停止并回收对应的 AudioSource 实例。
        /// </summary>
        public void StopPlayerWithClipByName(string clipName)
        {
            var clip = FindClipByName(clipName);
            if (clip != null) StopPlayerWithClip(clip);
        }

        /// <summary>
        /// 获取任意一个正在播放该 clip 的 AudioSource 实例。
        /// </summary>
        public AudioSource GetPlayerWithClip(AudioClip clip)
        {
            if (clip == null) return null;
            if (activePlayers.TryGetValue(clip, out var list) && list.Count > 0)
                return list[0];
            return null;
        }

        #endregion

        /// <summary>
        /// 播放指定 clip，可选择是否循环。
        /// </summary>
        private void Play(AudioClip clip, bool loop, float volume)
        {
            if (clip == null || pool == null) return;

            var src = pool.Get();
            src.clip = clip;
            src.loop = loop;
            src.volume = volume;
            src.Play();

            if (!activePlayers.TryGetValue(clip, out var list))
            {
                list = new List<AudioSource>();
                activePlayers[clip] = list;
            }
            list.Add(src);

            if (!loop)
                StartCoroutine(RecycleAfterDelay(src, clip.length));
        }

        private System.Collections.IEnumerator RecycleAfterDelay(AudioSource src, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReleaseAudioSource(src);
        }
    }
}