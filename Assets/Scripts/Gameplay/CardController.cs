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
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (model.isDone || !GameManager.Instance.isPlaying) return;
            if (!board.IsRowInteractable(model.row)) return;

            isDragging = true;
            originalAnchoredPos = rectTransform.anchoredPosition;
            
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

            HapticManager.Instance?.Play(HapticType.Light);
            
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
            
            wobbleTween?.Kill();
            transform.DORotate(Vector3.zero, 0.15f);
            
            UpdateSorting();

            CardController targetCard = board.HoveredCard;
            board.HideGhostCard();

            if (targetCard != null) {
                board.SwapCards(this, targetCard);
                HapticManager.Instance?.Play(HapticType.Medium);
            } else {
                rectTransform.DOAnchorPos(originalAnchoredPos, 0.25f).SetEase(Ease.OutBack);
                transform.DOScale(0.7f, 0.15f);
                HapticManager.Instance?.Play(HapticType.Soft);
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
    }
}