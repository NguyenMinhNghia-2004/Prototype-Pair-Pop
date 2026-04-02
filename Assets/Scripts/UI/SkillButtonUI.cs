using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PairPop.Skills;
using PairPop.Gameplay;
using PairPop.Core;
using DG.Tweening;

namespace PairPop.UI {
    /// <summary>
    /// UI Button cho mỗi skill ở dưới màn hình.
    /// Quản lý: hiển thị usage count, unlock flow, refill panel.
    /// </summary>
    public class SkillButtonUI : MonoBehaviour {
        [Header("Skill Reference")]
        public SkillBase skillAsset;
        public BoardController board;
        
        [Header("UI Elements")]
        public Image icon;
        public Button button;
        public TextMeshProUGUI usageCountText;        // Text hiển thị số lượt
        public GameObject lockedOverlay;               // Object che khi chưa unlock (icon ổ khóa)
        public GameObject bgIconHasTurns;              // Background icon khi CÒN lượt
        public GameObject bgIconNoTurns;               // Background icon khi HẾT lượt
        
        [Header("Skill Intro Panel (Unlock - Ảnh 1)")]
        [Tooltip("Panel giới thiệu skill khi unlock lần đầu")]
        public GameObject introPanel;
        public Image introSkillIcon;                   // Image để đổi icon skill
        public TextMeshProUGUI introSkillName;         // Tên skill
        public TextMeshProUGUI introSkillDesc;         // Mô tả skill
        public Button introContinueBtn;                // Nút Continue
        
        [Header("Skill Tutorial Panel (Ảnh 2)")]
        [Tooltip("Panel hướng dẫn sử dụng skill lần đầu")]
        public GameObject tutorialPanel;
        public TextMeshProUGUI tutorialText;           // Text hướng dẫn tùy theo skill
        public RectTransform tutorialSkillBtnAnchor;   // Anchor point để skill button nổi lên trên panel
        
        [Header("Refill Panel (Hết lượt - Ảnh 3)")]
        [Tooltip("Panel hiển thị khi hết lượt sử dụng")]
        public GameObject refillPanel;
        public Image refillSkillIcon;                  // Image để đổi icon skill
        public TextMeshProUGUI refillSkillName;        // Tên skill
        public TextMeshProUGUI refillSkillDesc;        // Mô tả skill
        public Button refillBuyCoinBtn;                // Mua bằng coin
        public Button refillWatchAdBtn;                // Xem quảng cáo
        public Button refillCloseBtn;                  // Đóng panel

        [Header("Refill Amounts")]
        public int coinRefillAmount = 3;
        public int coinRefillCost = 1000;
        public int adRefillAmount = 1;

        private bool isInTutorial = false;
        private Tween pulsingTween;
        private int originalSiblingIndex;
        private Transform originalParent;

        private void Start() {
            // Setup button listener
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnSkillClicked);
            
            // Setup intro panel buttons
            if (introContinueBtn != null) {
                introContinueBtn.onClick.RemoveAllListeners();
                introContinueBtn.onClick.AddListener(OnIntroContinue);
            }
            
            // Setup refill panel buttons
            if (refillBuyCoinBtn != null) {
                refillBuyCoinBtn.onClick.RemoveAllListeners();
                refillBuyCoinBtn.onClick.AddListener(OnRefillByCoin);
            }
            if (refillWatchAdBtn != null) {
                refillWatchAdBtn.onClick.RemoveAllListeners();
                refillWatchAdBtn.onClick.AddListener(OnRefillByAd);
            }
            if (refillCloseBtn != null) {
                refillCloseBtn.onClick.RemoveAllListeners();
                refillCloseBtn.onClick.AddListener(OnRefillClose);
            }
            
            // Lưu vị trí gốc
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            
            // Ẩn tất cả panels khi bắt đầu
            HideAllPanels();
            
            // Cập nhật UI ban đầu
            UpdateUI();
        }

        private void Update() {
            if (skillAsset == null || GameManager.Instance == null) return;
            UpdateUI();
        }

        #region UI Update

        /// <summary>
        /// Cập nhật UI hiển thị: locked/unlocked, usage count, background icons
        /// </summary>
        private void UpdateUI() {
            bool unlocked = skillAsset.IsUnlocked;
            bool hasTurns = unlocked && skillAsset.UsageCount > 0;
            
            // === Locked overlay ===
            if (lockedOverlay != null) {
                lockedOverlay.SetActive(!unlocked);
            }
            
            // === Background icons: ẩn hết nếu chưa unlock ===
            if (bgIconHasTurns != null) {
                bgIconHasTurns.SetActive(hasTurns);
            }
            if (bgIconNoTurns != null) {
                bgIconNoTurns.SetActive(unlocked && !hasTurns);
            }
            
            // === Usage count text: chỉ hiện khi có lượt ===
            if (usageCountText != null) {
                if (hasTurns) {
                    usageCountText.gameObject.SetActive(true);
                    usageCountText.text = skillAsset.UsageCount.ToString();
                } else {
                    usageCountText.gameObject.SetActive(false);
                }
            }
            
            // Button interactable khi đã unlock (dù hết lượt vẫn bấm được → hiện refill)
            button.interactable = unlocked;
            
            // Dim icon khi locked
            if (icon != null) {
                icon.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.6f);
            }
        }

        #endregion

        #region Unlock Flow

        /// <summary>
        /// Gọi từ BoardController sau khi chia bài xong.
        /// Nếu đủ level → unlock skill và hiện intro panel.
        /// </summary>
        public void TryUnlock(int currentLevelIndex) {
            if (skillAsset.IsUnlocked) return;
            if (currentLevelIndex < skillAsset.UnlockLevel) return;
            
            // Unlock skill: cộng 1 lượt mặc định
            skillAsset.IsUnlocked = true;
            skillAsset.AddUsage(1);
            
            ShowIntroPanel();
        }

        #endregion

        #region Intro Panel (Ảnh 1)

        /// <summary>
        /// Hiển thị panel giới thiệu skill
        /// </summary>
        private void ShowIntroPanel() {
            if (introPanel == null) return;
            
            // Pause game
            if (GameManager.Instance != null) {
                GameManager.Instance.isPlaying = false;
            }
            
            // Cập nhật nội dung từ SkillBase
            if (introSkillName != null) introSkillName.text = skillAsset.SkillName;
            if (introSkillDesc != null) introSkillDesc.text = skillAsset.IntroDescription;
            if (introSkillIcon != null && skillAsset.SkillIcon != null) {
                introSkillIcon.sprite = skillAsset.SkillIcon;
            }
            
            introPanel.SetActive(true);
            
            // Hiệu ứng xuất hiện: scale từ 0 bounce lên
            introPanel.transform.localScale = Vector3.zero;
            Sequence seq = DOTween.Sequence();
            seq.Append(introPanel.transform.DOScale(1.05f, 0.3f).SetEase(Ease.OutBack));
            seq.Append(introPanel.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutSine));
            seq.SetUpdate(true);
        }

        /// <summary>
        /// Bấm Continue → đóng intro → mở tutorial
        /// </summary>
        private void OnIntroContinue() {
            if (introPanel == null) return;
            
            introPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => {
                    introPanel.SetActive(false);
                    ShowTutorialPanel();
                });
        }

        #endregion

        #region Tutorial Panel (Ảnh 2)

        /// <summary>
        /// Hiển thị panel tutorial - skill button nổi lên trên panel và nhấp nháy
        /// </summary>
        private void ShowTutorialPanel() {
            if (tutorialPanel == null) {
                // Không có tutorial → unpause luôn
                if (GameManager.Instance != null) {
                    GameManager.Instance.isPlaying = true;
                }
                return;
            }
            
            isInTutorial = true;
            
            // Cập nhật text hướng dẫn từ SkillBase
            if (tutorialText != null) {
                tutorialText.text = skillAsset.TutorialDescription;
            }
            
            tutorialPanel.SetActive(true);
            
            // Hiệu ứng xuất hiện cho bubble
            tutorialPanel.transform.localScale = Vector3.zero;
            tutorialPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
            
            // === Đưa skill button lên trên tutorial panel (không bị che) ===
            BringButtonToFront();
            
            // === Hiệu ứng nhấp nháy co giãn liên tục trên skill button ===
            StartPulsingEffect();
            
            // Unpause game để người chơi có thể bấm skill
            if (GameManager.Instance != null) {
                GameManager.Instance.isPlaying = true;
            }
        }

        /// <summary>
        /// Đưa skill button lên trên cùng trong hierarchy để không bị tutorial panel che
        /// </summary>
        private void BringButtonToFront() {
            // Lưu vị trí gốc trước khi di chuyển
            originalSiblingIndex = transform.GetSiblingIndex();
            originalParent = transform.parent;
            
            // Đưa lên cuối hierarchy (render trên cùng)
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Trả skill button về vị trí gốc trong hierarchy
        /// </summary>
        private void RestoreButtonPosition() {
            if (originalParent != null) {
                transform.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1));
            }
        }

        /// <summary>
        /// Bắt đầu hiệu ứng co giãn nhấp nháy liên tục trên skill button
        /// </summary>
        private void StartPulsingEffect() {
            StopPulsingEffect();
            
            // Scale lên xuống liên tục
            pulsingTween = transform.DOScale(1.25f, 0.4f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        /// <summary>
        /// Dừng hiệu ứng nhấp nháy
        /// </summary>
        private void StopPulsingEffect() {
            if (pulsingTween != null) {
                pulsingTween.Kill();
                pulsingTween = null;
            }
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Đóng tutorial panel và dọn dẹp
        /// </summary>
        private void CloseTutorialPanel() {
            if (!isInTutorial || tutorialPanel == null) return;
            
            isInTutorial = false;
            StopPulsingEffect();
            RestoreButtonPosition();
            
            tutorialPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => tutorialPanel.SetActive(false));
        }

        #endregion

        #region Skill Button Click

        /// <summary>
        /// Khi bấm vào nút skill
        /// </summary>
        private void OnSkillClicked() {
            if (skillAsset == null) return;
            
            // Ẩn tutorial nếu đang hiện
            if (isInTutorial) {
                CloseTutorialPanel();
            }
            
            // Kiểm tra có lượt sử dụng không
            if (!skillAsset.CanUse) {
                // Hết lượt → hiện refill panel (ảnh 3)
                ShowRefillPanel();
                return;
            }
            
            // Kích hoạt skill
            skillAsset.Activate(board);
            HapticManager.Instance?.Play(HapticType.Medium);
            
            // Hiệu ứng icon bounce
            if (icon != null) {
                icon.transform.DOKill();
                icon.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
            }
            
            UpdateUI();
        }

        #endregion

        #region Refill Panel (Ảnh 3)

        /// <summary>
        /// Hiển thị refill panel khi hết lượt
        /// </summary>
        private void ShowRefillPanel() {
            if (refillPanel == null) return;
            
            // Pause game
            if (GameManager.Instance != null) {
                GameManager.Instance.isPlaying = false;
            }
            
            // Cập nhật nội dung từ SkillBase
            if (refillSkillName != null) refillSkillName.text = skillAsset.SkillName;
            if (refillSkillDesc != null) refillSkillDesc.text = skillAsset.RefillDescription;
            if (refillSkillIcon != null && skillAsset.SkillIcon != null) {
                refillSkillIcon.sprite = skillAsset.SkillIcon;
            }
            
            refillPanel.SetActive(true);
            
            // Hiệu ứng xuất hiện: scale bounce
            refillPanel.transform.localScale = Vector3.zero;
            Sequence seq = DOTween.Sequence();
            seq.Append(refillPanel.transform.DOScale(1.05f, 0.3f).SetEase(Ease.OutBack));
            seq.Append(refillPanel.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutSine));
            seq.SetUpdate(true);
        }

        /// <summary>
        /// Mua thêm lượt bằng coin
        /// </summary>
        private void OnRefillByCoin() {
            // TODO: Kiểm tra đủ coin không (kết nối với hệ thống coin)
            skillAsset.AddUsage(coinRefillAmount);
            AudioManager.Instance?.PlaySFX("Hint");
            HapticManager.Instance?.Play(HapticType.Medium);
            CloseRefillPanel();
        }

        /// <summary>
        /// Xem quảng cáo để nhận thêm lượt
        /// </summary>
        private void OnRefillByAd() {
            // TODO: Gọi Rewarded Ad SDK
            skillAsset.AddUsage(adRefillAmount);
            AudioManager.Instance?.PlaySFX("Hint");
            HapticManager.Instance?.Play(HapticType.Medium);
            CloseRefillPanel();
        }

        private void OnRefillClose() {
            CloseRefillPanel();
        }

        private void CloseRefillPanel() {
            if (refillPanel == null) return;
            
            refillPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => {
                    refillPanel.SetActive(false);
                    if (GameManager.Instance != null) {
                        GameManager.Instance.isPlaying = true;
                    }
                });
        }

        #endregion

        private void HideAllPanels() {
            if (introPanel != null) introPanel.SetActive(false);
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (refillPanel != null) refillPanel.SetActive(false);
        }

        private void OnDestroy() {
            StopPulsingEffect();
        }
    }
}
