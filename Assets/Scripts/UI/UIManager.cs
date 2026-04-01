using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PairPop.Core;
using DG.Tweening;

namespace PairPop.UI {
    public class UIManager : MonoBehaviour {
        public static UIManager Instance { get; private set; }

        [Header("Top Bar")]
        public TextMeshProUGUI levelLabel;
        
        [Header("Progress Bar")]
        public TextMeshProUGUI progressLabel; // "10/15"
        public Image progressFill;
        public RectTransform progressIcon; // Kéo thả icon (image bên cạnh thanh) vào đây
        public ParticleSystem textParticles1; // Kéo thả Particle System của Text vào đây
        public ParticleSystem textParticles2; // Kéo thả Particle System của Text vào đây


        
        [Header("Timer")]
        public TextMeshProUGUI timerLabel;

        [Header("Panels")]
        public GameObject settingsPanel;
        public GameObject winPanel;
        public GameObject losePanel;
        public GameObject pausePanel;

        [Header("Settings Buttons")]
        public Image musicBtnImg;
        public Image soundBtnImg;
        public Image vibrationBtnImg;

        private GameManager gm;
        private bool levelNameSet = false;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (textParticles1 != null) textParticles1.Stop();
            if (textParticles2 != null) textParticles2.Stop();
        }
        private void Start() {
            gm = GameManager.Instance;
            if (gm != null) {
                gm.OnTimeChanged += UpdateTime;
                gm.OnProgressChanged += UpdateProgress;

                if (gm.currentLevel != null) {
                    levelLabel.text = "Level " + (GameManager.currentLevelIndex + 1);
                    levelNameSet = true;
                    // Initialize progress at start if possible
                    UpdateProgress(gm.doneCount, gm.currentLevel.totalGroupCount);
                }
            }
            UpdateSettingsUI();
        }

        private void UpdateTime(float time) {
            if (!levelNameSet && gm != null && gm.currentLevel != null && levelLabel != null) {
                levelLabel.text = "Level " + (GameManager.currentLevelIndex + 1);
                levelNameSet = true;
            }

            if (timerLabel != null) {
                int minutes = Mathf.FloorToInt(Mathf.Max(0, time) / 60f);
                int seconds = Mathf.FloorToInt(Mathf.Max(0, time) % 60f);
                timerLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        private int lastDone = -1;

        private void UpdateProgress(int done, int total) {
            float delay = (lastDone >= 0 && done > lastDone) ? 0.6f : 0f;

            if (progressFill != null && total > 0) {
                float progress = (float)done / total;
                RectTransform rt = progressFill.rectTransform;
                
                rt.DOKill(); // Ngắt animation cũ (nếu có)
                rt.DOAnchorPosX(Mathf.Lerp(0f, 95f, progress), 0.35f).SetEase(Ease.OutQuad).SetDelay(delay);
                rt.DOSizeDelta(new Vector2(Mathf.Lerp(0f, 195f, progress), rt.sizeDelta.y), 0.35f).SetEase(Ease.OutQuad).SetDelay(delay);
            }

            // Hiệu ứng nhảy khi nhóm bài hoàn thành
            if (done > 0 && done > lastDone) {
                if (lastDone >= 0) {
                    // Delay toàn bộ hiệu ứng done để khớp animation bài bay về
                    DOVirtual.DelayedCall(0.6f, () => {
                        if (this == null) return; // Đề phòng object bị huỷ giữa chừng
                        
                        if (progressLabel != null) {
                            progressLabel.text = $"{done}/{total}";
                            progressLabel.rectTransform.DOKill(true);
                            progressLabel.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.35f, 5, 0.5f);
                        }
                        
                        if (progressIcon != null) {
                            progressIcon.DOKill(true);
                            progressIcon.DOPunchScale(Vector3.one * 0.25f, 0.4f, 6, 0.5f);
                            progressIcon.DOPunchRotation(new Vector3(0, 0, 15f), 0.4f, 6, 0.5f);
                        }
                        
                        if (textParticles1 != null && textParticles2 != null) {
                            textParticles1.Stop(); 
                            textParticles1.Play();
                            textParticles2.Stop(); 
                            textParticles2.Play();
                        }
                    });
                }
            } else {
                // Update ngay lập tức lúc mới vào level
                if (progressLabel != null) {
                    progressLabel.text = $"{done}/{total}";
                }
            }
            lastDone = done;
        }

        #region Buttons and Toggles

        public void ShowWinPanel() {
            if (winPanel != null) winPanel.SetActive(true);
        }

        public void ShowLosePanel() {
            if (losePanel != null) losePanel.SetActive(true);
        }

        public void NextLevelBtn() {
            if (gm != null) {
                gm.NextLevel();
            }
        }
        
        public void TogglePause() {
            if (pausePanel != null) {
                bool isActive = !pausePanel.activeSelf;
                pausePanel.SetActive(isActive);
                if (gm != null) {
                    gm.isPlaying = !isActive;
                }
            }
        }

        public void ToggleSettingsPanel() {
            if (settingsPanel != null) {
                bool isActive = !settingsPanel.activeSelf;
                settingsPanel.SetActive(isActive);
                if (isActive) {
                    UpdateSettingsUI();
                }
            }
        }

        private void UpdateSettingsUI() {
            if (AudioManager.Instance != null) {
                if (musicBtnImg != null) musicBtnImg.color = AudioManager.Instance.isMusicEnabled ? Color.white : Color.gray;
                if (soundBtnImg != null) soundBtnImg.color = AudioManager.Instance.isSFXEnabled ? Color.white : Color.gray;
            }
            if (HapticManager.Instance != null && vibrationBtnImg != null) {
                vibrationBtnImg.color = HapticManager.Instance.isEnabled ? Color.white : Color.gray;
            }
        }

        public void ToggleMusic() {
            if (gm != null) {
                gm.ToggleMusic();
                UpdateSettingsUI();
            }
            else Debug.LogError("GameManager is null");
        }

        public void ToggleSound() {
            if (gm != null) {
                gm.ToggleSound();
                UpdateSettingsUI();
            }
             else Debug.LogError("GameManager is null");
        }

        public void ToggleVibration() {
            if (gm != null) {
                gm.ToggleVibration();
                UpdateSettingsUI();
            }
             else Debug.LogError("GameManager is null");
        }

        public void Replay() {
            if (gm != null) {
                gm.Replay();
            }
             else Debug.LogError("GameManager is null");
        }

        #endregion

        private void OnDestroy() {
            if (gm != null) {
                gm.OnTimeChanged -= UpdateTime;
                gm.OnProgressChanged -= UpdateProgress;
            }
        }
    }
}
