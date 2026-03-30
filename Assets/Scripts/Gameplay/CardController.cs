using UnityEngine;
using DG.Tweening;
using PairPop.Models;
using PairPop.Core;

namespace PairPop.Gameplay {
    public class CardController : MonoBehaviour {
        [Header("References")]
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer backgroundRenderer; // Để đổi màu khi Done/Skill
        public SpriteRenderer borderGlow; // Dành cho skill Highlight
        
        [Header("Card Data")]
        public CardModel model;
        
        // Trạng thái Drag
        private bool isDragging;
        private Vector3 originalPos;
        private Vector3 pointerOffset;
        
        // Reference tới Board
        private BoardController board;

        public void Init(CardModel data, BoardController boardCtrl, Sprite frontSprite) {
            model = data;
            board = boardCtrl;
            spriteRenderer.sprite = frontSprite;
            borderGlow.color = new Color(1, 1, 1, 0); // Ẩn viền glow khởi đầu
        }

        private void OnMouseDown() {
            if (model.isDone || !GameManager.Instance.isPlaying) return;
            
            // Chỉ hàng dưới cùng (hoặc hàng hiện tại có thể tương tác) mới được kéo
            if (!board.IsRowInteractable(model.row)) return;

            isDragging = true;
            originalPos = transform.position;
            pointerOffset = transform.position - GetMouseWorldPos();

            // Nhấc lên effect
            transform.DOScale(1.15f, 0.12f).SetEase(Ease.OutBack);
            // Sorting order lên cao nhất để không bị đè
            GetComponent<Renderer>().sortingOrder = 100;

            HapticManager.Instance?.Play(HapticType.Light);
            // AudioManager.Instance?.PlaySFX("pick_up");
            
            // Có thể thả thêm ghost card tại đây như design doc
            board.ShowGhostCard(originalPos);
        }

        private void OnMouseDrag() {
            if (!isDragging) return;

            Vector3 newPos = GetMouseWorldPos() + pointerOffset;
            transform.position = newPos;

            // Lắc theo tốc độ kéo
            float mouseMovementX = Input.GetAxis("Mouse X");
            float wobble = mouseMovementX * 5f;
            transform.rotation = Quaternion.Euler(0, 0, wobble);
            
            // Hover check
            board.CheckHover(this, newPos);
        }

        private void OnMouseUp() {
            if (!isDragging) return;
            isDragging = false;

            transform.DORotate(Vector3.zero, 0.15f);
            GetComponent<Renderer>().sortingOrder = model.row + 10; // Trả lại thứ tự chuẩn

            board.HideGhostCard();

            // Tìm thẻ bị thả vào để swap
            CardController targetCard = board.GetCardAtMousePos(GetMouseWorldPos());

            if (targetCard != null && targetCard != this && !targetCard.model.isDone && targetCard.model.row == model.row) {
                // Hợp lệ, tiến hành Swap
                board.SwapCards(this, targetCard);
                HapticManager.Instance?.Play(HapticType.Medium);
                // AudioManager.Instance?.PlaySFX("drop_success");
            } else {
                // Fail ngưng kéo
                transform.DOMove(originalPos, 0.25f).SetEase(Ease.OutBack);
                transform.DOScale(1.0f, 0.15f);
                HapticManager.Instance?.Play(HapticType.Soft);
                // AudioManager.Instance?.PlaySFX("drop_fail");
                GameManager.Instance.ResetCombo(); // Swap sai -> mất combo
            }
        }

        private Vector3 GetMouseWorldPos() {
            Vector3 mousePoint = Input.mousePosition;
            mousePoint.z = Mathf.Abs(Camera.main.transform.position.z);
            return Camera.main.ScreenToWorldPoint(mousePoint);
        }

        public void HighlightHover(bool isHover) {
            // Hover effect
            float targetAlpha = isHover ? 0.6f : 0f;
            borderGlow.DOFade(targetAlpha, 0.1f);
        }

        public void FlashBackground(Color color, float duration) {
            backgroundRenderer.DOColor(color, duration).SetLoops(2, LoopType.Yoyo);
        }
    }
}
