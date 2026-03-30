using UnityEngine;
using PairPop.Data;
using System;

namespace PairPop.Core {
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        public LevelDataSO currentLevel;
        
        [Header("State")]
        public bool isPlaying;
        public float currentTime;
        public int score;
        public float comboMultiplier = 1f;
        public int currentComboCount = 0;
        public int doneCount;

        public Action<int> OnScoreChanged;
        public Action<float> OnComboChanged;
        public Action<float> OnTimeChanged;
        public Action OnLevelComplete;

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartLevel(LevelDataSO level) {
            currentLevel = level;
            currentTime = level.timeLimit;
            score = 0;
            comboMultiplier = 1f;
            currentComboCount = 0;
            doneCount = 0;
            isPlaying = true;

            // Truyền event cho các UI lắng nghe
            OnScoreChanged?.Invoke(score);
            OnComboChanged?.Invoke(comboMultiplier);
            OnTimeChanged?.Invoke(currentTime);
        }

        private void Update() {
            // Có thể dùng một biến global dạng IsTimeFrozen từ SkillFrozen
            if (!isPlaying) return;

            // Xử lý Time limit
            if (currentLevel.timeLimit > 0) {
                currentTime -= Time.deltaTime;
                OnTimeChanged?.Invoke(currentTime);
                if (currentTime <= 0) {
                    isPlaying = false;
                    Debug.Log("Time's up! Level Failed.");
                    // Xử lý game over ở đây
                }
            }
        }

        public void AddScoreForDoneRow() {
            int baseScore = 100;
            int finalScore = Mathf.RoundToInt(baseScore * comboMultiplier);
            score += finalScore;
            
            doneCount++;
            IncreaseCombo();

            OnScoreChanged?.Invoke(score);

            if (doneCount >= currentLevel.totalGroupCount) {
                isPlaying = false;
                OnLevelComplete?.Invoke();
                Debug.Log("LEVEL COMPLETE!");
            }
        }

        public void IncreaseCombo() {
            currentComboCount++;
            if (currentComboCount >= 2) {
                comboMultiplier += 0.5f;
            }
            OnComboChanged?.Invoke(comboMultiplier);
        }

        public void ResetCombo() {
            currentComboCount = 0;
            comboMultiplier = 1f;
            OnComboChanged?.Invoke(comboMultiplier);
        }
    }
}
