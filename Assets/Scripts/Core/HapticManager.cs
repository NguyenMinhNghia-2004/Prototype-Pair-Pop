using UnityEngine;

namespace PairPop.Core {
    public enum HapticType { Light, Soft, Medium, Heavy, Success, Warning, Error }

    public class HapticManager : MonoBehaviour {
        public static HapticManager Instance { get; private set; }
        public bool isEnabled = true;

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Play(HapticType type) {
            if (!isEnabled) return;
            
            // Xử lý tạm thời sài Handheld.Vibrate của Unity. 
            // Nếu dùng NiceVibrations hoặc plugin khác, hãy thay thế logic ở đây.
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            Handheld.Vibrate();
            #endif
            
            Debug.Log($"[Haptic] Played vibration type: {type}");
        }
    }
}
