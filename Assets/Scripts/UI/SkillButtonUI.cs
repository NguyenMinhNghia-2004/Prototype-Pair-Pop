using UnityEngine;
using UnityEngine.UI;
using PairPop.Skills;
using PairPop.Gameplay;
using PairPop.Core;
using DG.Tweening;

namespace PairPop.UI {
    public class SkillButtonUI : MonoBehaviour {
        public ScriptableObject skillAsset; // ISkill reference
        public BoardController board;       // Gán board vào để skill biết tác động lên bài nào
        
        [Header("UI Elements")]
        public Image icon;
        public Image fillCooldown;
        public Button buttonBtn;

        private ISkill _skill;

        private void Start() {
            _skill = skillAsset as ISkill;
            if (_skill == null) {
                Debug.LogError("Chưa gán đúng ISkill cho Button");
                buttonBtn.interactable = false;
                return;
            }
            
            // Xoá listener cũ, thêm listener mới
            buttonBtn.onClick.RemoveAllListeners();
            buttonBtn.onClick.AddListener(OnSkillClicked);
        }

        private void Update() {
            if (_skill == null || GameManager.Instance == null) return;

            int currentDone = GameManager.Instance.doneCount;
            float progress = _skill.CurrentCooldownProgress(currentDone);
            
            fillCooldown.fillAmount = 1f - progress;
            buttonBtn.interactable = _skill.IsReady(currentDone);
            
            // Thêm hiệu ứng lắc lư nếu sẵn sàng
            if (progress >= 1f && DOTween.IsTweening(icon.transform) == false) {
                // icon.transform.DOPunchRotation(new Vector3(0,0,10), 0.5f).SetLoops(-1, LoopType.Restart);
            }
        }

        private void OnSkillClicked() {
            if (_skill == null) return;
            
            _skill.Activate(board);
            HapticManager.Instance?.Play(HapticType.Medium);
            
            // Ngừng lắc
            // icon.transform.DOKill();
            // icon.transform.rotation = Quaternion.identity;
        }
    }
}
