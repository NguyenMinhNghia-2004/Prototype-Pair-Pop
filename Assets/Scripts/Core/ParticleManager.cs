using UnityEngine;

namespace PairPop.Core {
    public class ParticleManager : MonoBehaviour {
        public static ParticleManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject starBurstPrefab;
        [SerializeField] private GameObject confettiPrefab;
        [SerializeField] private GameObject magicSparklePrefab;
        
        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Burst(Vector3 position, Color color) {
            if (starBurstPrefab == null) return;
            GameObject burst = Instantiate(starBurstPrefab, position, Quaternion.identity);
            
            // Nếu có ParticleSystem, thử chỉnh màu (yêu cầu cấu hình thêm trên prefab)
            ParticleSystem ps = burst.GetComponent<ParticleSystem>();
            if (ps != null) {
                var main = ps.main;
                main.startColor = color;
            }
            
            Destroy(burst, 2f);
        }

        public void PlayConfetti(Vector3 position) {
            if (confettiPrefab == null) return;
            GameObject fx = Instantiate(confettiPrefab, position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        public void PlayMagicSparkle(Vector3 position) {
            if (magicSparklePrefab == null) return;
            GameObject fx = Instantiate(magicSparklePrefab, position, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }
}
