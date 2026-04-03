using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using PairPop.Models;
using PairPop.Core;

namespace PairPop.Gameplay {
    public class CardController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
        [Header("References")]
        public Image itemRenderer;       // Changed from SpriteRenderer to Image
        public Image backgroundRenderer; // Changed from SpriteRenderer to Image
        public GameObject dashedLineObj;
        public GameObject cardBackObj;
        
        [Header("Card Data")]
        public CardModel model;
        
        // UI Drag consistency
        private RectTransform rectTransform;
        private Canvas canvas;
        private bool isDragging;
        private Vector2 originalAnchoredPos;
        private Vector2 pointerOffset;
        
        // Reference tới Board
        private BoardController board;
        private Tween wobbleTween;
        private Tween returnTween; // Track tween trở về vị trí cũ

        private void Awake() {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void Init(CardModel data, BoardController boardCtrl, Sprite frontSprite) {
            model = data;
            board = boardCtrl;
            itemRenderer.sprite = frontSprite;
            
            if (dashedLineObj != null) {
                dashedLineObj.SetActive(false);
            }
            
            // Mặc định hiện mặt trước
            ShowFront();
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (model.isDone || !GameManager.Instance.isPlaying) return;
            if (!board.IsRowInteractable(model.row)) return;
            
            // === FIX Bug 1: Block tương tác khi board đang animate ===
            if (board.IsAnimating) return;
            
            AudioManager.Instance.PlaySFX("Select");
            isDragging = true;
            
            // === FIX Bug 2: Kill tween cũ nếu đang animate trở về ===
            returnTween?.Kill();
            
            // === FIX Bug 2: Luôn tính vị trí gốc từ grid, không dùng anchoredPosition hiện tại ===
            originalAnchoredPos = board.GetCellPosition(model.row, model.col);
            
            // Snap card về đúng vị trí grid trước khi bắt đầu kéo (phòng trường hợp đang giữa animation)
            rectTransform.anchoredPosition = originalAnchoredPos;
            
            // Calculate pointer offset in local anchored position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector2 localPoint
            );
            pointerOffset = rectTransform.anchoredPosition - localPoint;

            // Nhấc lên effect
            transform.DOScale(0.8f, 0.12f).SetEase(Ease.OutBack);
            
            // Lắc lư effect
            wobbleTween?.Kill();
            transform.localRotation = Quaternion.Euler(0, 0, -5f);
            wobbleTween = transform.DORotate(new Vector3(0, 0, 5f), 0.25f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            
            // Bring to front in UI
            transform.SetAsLastSibling();

            HapticManager.Instance?.Play(HapticType.Medium);
            
            // Thả ghost card tại đây
            board.ShowGhostCard(transform.position);
        }

        public void OnDrag(PointerEventData eventData) {
            if (!isDragging) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector2 localPoint
            );
            
            rectTransform.anchoredPosition = localPoint + pointerOffset;

            // Hover check - passing world position for consistency with board logic or we can update board to use UI pos
            board.CheckHover(this, transform.position);
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (!isDragging) return;
            isDragging = false;
            AudioManager.Instance.PlaySFX("Drop");
            wobbleTween?.Kill();
            transform.DORotate(Vector3.zero, 0.15f);
            
            UpdateSorting();

            CardController targetCard = board.HoveredCard;
            board.HideGhostCard();

            if (targetCard != null) {
                board.SwapCards(this, targetCard);
                HapticManager.Instance?.Play(HapticType.Medium);
            } else {
                // === FIX Bug 2: Track return tween để có thể kill nếu click lại nhanh ===
                returnTween = rectTransform.DOAnchorPos(originalAnchoredPos, 0.25f).SetEase(Ease.OutBack);
                transform.DOScale(0.7f, 0.15f);
                HapticManager.Instance?.Play(HapticType.Medium);
                GameManager.Instance.ResetCombo();
            }
            
            HighlightHover(false);
        }

        public void HighlightHover(bool isHover) {
            if (dashedLineObj != null) {
                dashedLineObj.SetActive(isHover);
            }
        }

        public void UpdateSorting() {
            // In UI, sorting is mainly sibling index.
            // For a grid, we might want to keep them in a specific order when not dragging.
            // But usually, the order doesn't matter unless they overlap.
            // If they overlap (rows), we can manage index.
            // transform.SetSiblingIndex(model.row * 4 + model.col);
        }

        public void FlashBackground(Color color, float duration) {
            backgroundRenderer.DOColor(color, duration).SetLoops(2, LoopType.Yoyo);
        }

        /// <summary>
        /// Đổi sprite background khi done
        /// </summary>
        public void SetDoneBackground(Sprite doneSprite) {
            if (doneSprite != null) {
                backgroundRenderer.sprite = doneSprite;
            }
        }

        /// <summary>
        /// Đổi sprite background khi Hint Booster gợi ý (card vẫn active, chưa done).
        /// Dùng sprite done color để gợi ý cho người chơi biết 2 card cùng group.
        /// </summary>
        public void SetHintDoneBackground(Sprite hintSprite) {
            if (hintSprite != null) {
                backgroundRenderer.sprite = hintSprite;
            }
        }

        /// <summary>
        /// Hiện mặt trước card (item + background), ẩn cardBack
        /// </summary>
        public void ShowFront() {
            if (itemRenderer != null) itemRenderer.gameObject.SetActive(true);
            if (cardBackObj != null) cardBackObj.SetActive(false);
        }

        /// <summary>
        /// Hiện mặt sau card (cardBack), ẩn item
        /// </summary>
        public void ShowBack() {
            if (itemRenderer != null) itemRenderer.gameObject.SetActive(false);
            if (cardBackObj != null) cardBackObj.SetActive(true);
        }

        /// <summary>
        /// Set sprite cho cardBack (mặt sau)
        /// </summary>
        public void SetCardBackSprite(Sprite backSprite) {
            if (cardBackObj != null && backSprite != null) {
                var img = cardBackObj.GetComponent<Image>();
                if (img != null) img.sprite = backSprite;
            }
        }

        /// <summary>
        /// Animation lật card: mặt trước → mặt sau
        /// Scale X: 1 → 0 (ẩn mặt trước) → 0 → 1 (hiện mặt sau)
        /// </summary>
        public Tween FlipToBack(float duration = 0.3f) {
            Sequence flipSeq = DOTween.Sequence();
            flipSeq.Append(transform.DOScaleX(0f, duration * 0.5f).SetEase(Ease.InSine));
            flipSeq.AppendCallback(() => {
                ShowBack();
            });
            flipSeq.Append(transform.DOScaleX(transform.localScale.x > 0 ? 0.7f : 1f, duration * 0.5f).SetEase(Ease.OutSine));
            return flipSeq;
        }

        /// <summary>
        /// Animation lật card: mặt sau → mặt trước
        /// </summary>
        public Tween FlipToFront(float duration = 0.3f) {
            Sequence flipSeq = DOTween.Sequence();
            flipSeq.Append(transform.DOScaleX(0f, duration * 0.5f).SetEase(Ease.InSine));
            flipSeq.AppendCallback(() => {
                ShowFront();
            });
            flipSeq.Append(transform.DOScaleX(transform.localScale.x > 0 ? 0.7f : 1f, duration * 0.5f).SetEase(Ease.OutSine));
            return flipSeq;
        }

        /// <summary>
        /// Kill all running tweens related to this card.
        /// </summary>
        public void KillAllTweens() {
            returnTween?.Kill();
            wobbleTween?.Kill();
            rectTransform.DOKill();
            transform.DOKill();
        }
    }
}