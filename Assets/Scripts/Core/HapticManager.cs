using UnityEngine;
using CandyCoded.HapticFeedback; // Thêm namespace của plugin

namespace PairPop.Core {
    public enum HapticType { Light, Medium, Heavy}

    public class HapticManager : MonoBehaviour {
        public static HapticManager Instance { get; private set; }
        
        [Header("Settings")]
        public bool isEnabled = true;
        [Range(0f, 1f)] public float globalIntensity = 1f; // Điều chỉnh cường độ chung

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }

        public void Play(HapticType type) {
            // Kiểm tra setting và cường độ global
            if (!isEnabled || globalIntensity <= 0.1f) return;
            
            // Gọi hàm rung từ plugin dựa trên HapticType
            VibrateWithPlugin(type);
            
            // Debug.Log($"[Haptic] Played vibration type: {type}");
        }

        private void VibrateWithPlugin(HapticType type) {
            #if UNITY_EDITOR
                // Debug.Log($"[Haptic-Sim] {type}");
            #else
                switch (type) {
                    case HapticType.Light:
                        HapticFeedback.LightFeedback();
                        break;
                    case HapticType.Medium:
                        HapticFeedback.MediumFeedback();
                        break;
                    case HapticType.Heavy:
                        HapticFeedback.HeavyFeedback();
                        break;
                }
            #endif
        }

        public void SetGlobalIntensity(float value) {
            globalIntensity = Mathf.Clamp01(value);
        }
    }
}
