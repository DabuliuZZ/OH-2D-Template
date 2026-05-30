using Sirenix.OdinInspector;
using UnityEngine;

namespace OHTools
{
    /// <summary>
    /// 屏幕扭曲后处理效果，类似 Minecraft 反胃效果。
    /// 挂载到 Camera 上即可生效。
    /// </summary>
    public class OHDistortWarpPostProcessing : MonoBehaviour
    {
        [LabelText("扭曲材质（自动创建）"), SerializeField, ReadOnly]
        private Material _distortMaterial;

        [LabelText("扭曲强度"), SerializeField, Range(0f, 0.1f)]
        private float _distortStrength = 0f;

        [LabelText("扭曲速度"), SerializeField, Range(0f, 10f)]
        private float _distortSpeed = 0f;

        [LabelText("扭曲频率"), SerializeField, Range(1f, 50f)]
        private float _distortFrequency = 1f;

        [LabelText("是否启用"), SerializeField]
        private bool _isEnabled = true;

        /// <summary>
        /// 启用或禁用扭曲效果。
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// 扭曲强度（0~0.1）。
        /// </summary>
        public float DistortStrength
        {
            get => _distortStrength;
            set => _distortStrength = Mathf.Clamp(value, 0f, 0.1f);
        }

        /// <summary>
        /// 扭曲速度（0~10）。
        /// </summary>
        public float DistortSpeed
        {
            get => _distortSpeed;
            set => _distortSpeed = Mathf.Clamp(value, 0f, 10f);
        }

        /// <summary>
        /// 扭曲频率（1~50）。
        /// </summary>
        public float DistortFrequency
        {
            get => _distortFrequency;
            set => _distortFrequency = Mathf.Clamp(value, 1f, 50f);
        }

        private void Start()
        {
            _distortMaterial = new Material(Shader.Find("OHEffect/OHDistortWarp"));
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_distortMaterial != null && _isEnabled)
            {
                _distortMaterial.SetFloat("_DistortStrength", _distortStrength);
                _distortMaterial.SetFloat("_DistortSpeed", _distortSpeed);
                _distortMaterial.SetFloat("_DistortFrequency", _distortFrequency);
                Graphics.Blit(src, dest, _distortMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        private void OnDestroy()
        {
            if (_distortMaterial != null)
                Destroy(_distortMaterial);
        }
    }
}