using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PairPop.Core;

namespace PairPop.UI {
    public class UIManager : MonoBehaviour {
        [Header("Top Bar")]
        public TextMeshProUGUI scoreLabel;
        public TextMeshProUGUI levelLabel;
        
        [Header("Timer Bar")]
        public GameObject timerContainer;
        public Image timerFill;
        public TextMeshProUGUI timerLabel;

        [Header("Combo Effect")]
        public GameObject comboPopupPrefab; // Popup hiện x2, x3...
        public Canvas mainCanvas;

        private GameManager gm;
        private float initialTime;

        private void Start() {
            gm = GameManager.Instance;
            gm.OnScoreChanged += UpdateScore;
            gm.OnTimeChanged += UpdateTime;
            gm.OnComboChanged += UpdateCombo;
            gm.OnLevelComplete += ShowLevelComplete;

            if (gm.currentLevel != null) {
                levelLabel.text = "LEVEL " + (gm.currentLevel.name); // Tên ví dụ
                initialTime = gm.currentLevel.timeLimit;
                timerContainer.SetActive(initialTime > 0);
            }
        }

        private void UpdateScore(int newScore) {
            scoreLabel.text = newScore.ToString();
            scoreLabel.rectTransform.localScale = Vector3.one * 1.5f;
            scoreLabel.rectTransform.LeanScale(Vector3.one, 0.2f).setEaseOutBounce(); // Fallback nếu xài LeanTween, hoặc dùng DOTween ở dưới
            // scoreLabel.rectTransform.DOScale(1f, 0.2f);
        }

        private void UpdateTime(float time) {
            if (initialTime > 0) {
                timerLabel.text = Mathf.CeilToInt(time).ToString() + "s";
                timerFill.fillAmount = time / initialTime;
            }
        }

        private void UpdateCombo(float mult) {
            if (mult > 1f) {
                // Tạo popup combo giữa màn hình 
                if (comboPopupPrefab != null) {
                    GameObject go = Instantiate(comboPopupPrefab, mainCanvas.transform);
                    var text = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null) text.text = "COMBO x" + ((mult - 1) * 2 + 1) + "!"; 
                    Destroy(go, 1.5f);
                }
            }
        }

        private void ShowLevelComplete() {
            Debug.Log("[UIManager] HIỆN BẢNG THẮNG TRẬN - DONE!");
        }

        private void OnDestroy() {
            if (gm != null) {
                gm.OnScoreChanged -= UpdateScore;
                gm.OnTimeChanged -= UpdateTime;
                gm.OnComboChanged -= UpdateCombo;
                gm.OnLevelComplete -= ShowLevelComplete;
            }
        }
    }
}
