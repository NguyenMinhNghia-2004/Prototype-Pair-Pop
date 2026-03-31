using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace PairPop.Gameplay {
    /// <summary>
    /// Controller cho prefab Done Box - hiển thị khi 1 group hoàn thành.
    /// Cấu trúc prefab (theo hierarchy trong Unity):
    ///   DoneBox (RectTransform + DoneBoxController)
    ///     ├─ BG Box    (Image - swap bgBoxSprite)
    ///     ├─ card1     (Image - swap backCardSprite)
    ///     │    └─ Image1 (Image - item sprite 0)
    ///     ├─ card2     (Image - swap backCardSprite)
    ///     │    └─ Image2 (Image - item sprite 1)
    ///     ├─ card3     (Image - swap backCardSprite)
    ///     │    └─ Image3 (Image - item sprite 2)
    ///     ├─ card4     (Image - swap backCardSprite)
    ///     │    └─ Image4 (Image - item sprite 3)
    ///     └─ Box Title (Image - swap ribbonSprite)
    ///          └─ Text (TMP) (tên group)
    /// </summary>
    public class DoneBoxController : MonoBehaviour {
        [Header("References")]
        public Image bgBoxImage;
        [Tooltip("Image của card1, card2, card3, card4 - background của từng card")]
        public Image[] cardBgImages = new Image[4];
        [Tooltip("Image của Image1, Image2, Image3, Image4 - item sprite bên trong card")]
        public Image[] itemImages = new Image[4];
        public Image boxTitleImage;
        public TextMeshProUGUI titleText;

        /// <summary>
        /// Khởi tạo Done Box:
        /// - Swap sprite BG Box, 4 card bg, Box Title (Ribbon)
        /// - Gán 4 sprite của group vào Item1-4
        /// - Gán tên group vào text
        /// </summary>
        public void Init(Data.GroupDataSO group, Sprite backCardSprite, Sprite bgBoxSprite, Sprite ribbonSprite) {
            // Swap BG Box sprite
            if (bgBoxImage != null && bgBoxSprite != null) {
                bgBoxImage.sprite = bgBoxSprite;
            }

            // Swap 4 card background sprites
            for (int i = 0; i < 4 && i < cardBgImages.Length; i++) {
                if (cardBgImages[i] != null && backCardSprite != null) {
                    cardBgImages[i].sprite = backCardSprite;
                }
            }

            // Gán 4 item sprites
            for (int i = 0; i < 4 && i < itemImages.Length; i++) {
                if (itemImages[i] != null && i < group.sprites.Length) {
                    itemImages[i].sprite = group.sprites[i];
                }
            }

            // Swap Ribbon (Box Title) sprite
            if (boxTitleImage != null && ribbonSprite != null) {
                boxTitleImage.sprite = ribbonSprite;
            }

            // Set tên group
            if (titleText != null) {
                titleText.text = group.groupName;
            }
        }

        /// <summary>
        /// Hiệu ứng xuất hiện: scale bounce lên tăng feeling
        /// </summary>
        public void PlayAppearAnimation(float delay = 0f) {
            transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.SetDelay(delay);
            
            // Scale nảy lên từ 0 → 0.65 (overshoot mạnh)
            seq.Append(transform.DOScale(0.65f, 0.35f).SetEase(Ease.OutBack, 1.5f));
            
            // Nảy nhẹ thêm 1 lần để tạo cảm giác bouncy
            seq.Append(transform.DOScale(0.5f, 0.15f).SetEase(Ease.InOutSine));
            seq.Append(transform.DOScale(0.55f, 0.1f).SetEase(Ease.OutSine));
            seq.Append(transform.DOScale(0.5f, 0.08f).SetEase(Ease.InSine));
        }
    }
}
