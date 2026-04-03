using UnityEngine;
using PairPop.Data;
using System;
using UnityEngine.SceneManagement;
using PairPop.UI;
using System.Collections;
using PairPop.Skills;

namespace PairPop.Core {
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        [Header("Levels Setup")]
        public LevelDataSO[] levels;
        public static int currentLevelIndex = 0;

        [HideInInspector]
        public LevelDataSO currentLevel;
        
        [Header("Effects")]
        public ParticleSystem[] winParticles;
        
        [Header("State")]
        public bool isPlaying;
        public bool isFrozen;  // Skill Frozen: tạm dừng trừ thời gian
        public float currentTime;
        public int score;
        public float comboMultiplier = 1f;
        public int currentComboCount = 0;
        public int doneCount;

        public Action<int> OnScoreChanged;
        public Action<float> OnComboChanged;
        public Action<float> OnTimeChanged;
        public Action<int, int> OnProgressChanged; 

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

            if (levels != null && levels.Length > 0) {
                currentLevel = levels[currentLevelIndex % levels.Length];
            }

             if (winParticles != null) {
                foreach (var p in winParticles) {
                    if (p != null) p.Stop();
                }
            }
        }

        public void StartLevel(LevelDataSO level) {
            currentLevel = level;
            currentTime = level.timeLimit;
            score = 0;
            comboMultiplier = 1f;
            currentComboCount = 0;
            doneCount = 0;
            isPlaying = true;
            isFrozen = false;

            // Truyền event cho các UI lắng nghe
            OnScoreChanged?.Invoke(score);
            OnComboChanged?.Invoke(comboMultiplier);
            OnTimeChanged?.Invoke(currentTime);
            OnProgressChanged?.Invoke(doneCount, currentLevel.totalGroupCount);
        }

        private void Update() {
            if (!isPlaying) return;

            // Xử lý Time limit
            if (currentLevel != null && currentLevel.timeLimit > 0 && !isFrozen) {
                currentTime -= Time.deltaTime;
                OnTimeChanged?.Invoke(currentTime);
                if (currentTime <= 0) {
                    LoseGame();
                }
            }
        }

        private void LoseGame() {
            isPlaying = false;
            Debug.Log("Time's up! Level Failed.");
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowLosePanel();
            }
        }

        public void AddScoreForDoneRow() {
            int baseScore = 100;
            int finalScore = Mathf.RoundToInt(baseScore * comboMultiplier);
            score += finalScore;
            
            doneCount++;
            IncreaseCombo();

            OnScoreChanged?.Invoke(score);
            OnProgressChanged?.Invoke(doneCount, currentLevel.totalGroupCount);

            if (doneCount >= currentLevel.totalGroupCount) {
                StartCoroutine(WinGame());
            }
        }
        IEnumerator WinGame() {
            isPlaying = false;
            Debug.Log("LEVEL COMPLETE!");
            yield return new WaitForSeconds(1f);
            // Xử lý bật effect particles
            if (winParticles != null) {
                foreach (var p in winParticles) {
                    if (p != null) p.Play();
                }
            }

            if (UIManager.Instance != null) {
                yield return new WaitForSeconds(1.8f);
                UIManager.Instance.ShowWinPanel();
                
                foreach (var p in winParticles) {
                    if (p != null) p.Stop();
                }
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

        #region UI Operations & Core Flow
        public void NextLevel() {
            currentLevelIndex++;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ToggleSound() {
            if(AudioManager.Instance != null) {
                AudioManager.Instance.ToggleSFX();
            }
        }

        public void ToggleMusic() {
            if(AudioManager.Instance != null) {
                AudioManager.Instance.ToggleMusic();
            }
        }

        public void ToggleVibration() {
            if(HapticManager.Instance != null) {
                HapticManager.Instance.isEnabled = !HapticManager.Instance.isEnabled;
            }
        }

        public void Replay() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        #endregion
    }
}
