using UnityEngine;
using PairPop.Gameplay;
using System.Collections;
using DG.Tweening;

namespace PairPop.Skills {
    [CreateAssetMenu(menuName = "PairPop/Skills/Frozen (Time Freeze)")]
    public class SkillFrozen : ScriptableObject, ISkill {
        public int unlockLevel = 16;
        public int cooldownTurns = 5;
        public float duration = 20f;
        
        private int lastUsedDoneCount = -cooldownTurns;

        public int UnlockLevel => unlockLevel;
        public int CooldownTurns => cooldownTurns;

        public bool IsReady(int currentDoneCount) {
            return (currentDoneCount - lastUsedDoneCount) >= cooldownTurns;
        }

        public float CurrentCooldownProgress(int currentDoneCount) {
            float diff = currentDoneCount - lastUsedDoneCount;
            return Mathf.Clamp01(diff / cooldownTurns);
        }

        public void Activate(BoardController board) {
            lastUsedDoneCount = Core.GameManager.Instance.doneCount;

            // Chuyển GameManager về chế độ không tính thời gian
            board.StartCoroutine(FreezeRoutine());
        }

        private IEnumerator FreezeRoutine() {
            var gm = Core.GameManager.Instance;
            bool wasPlaying = gm.isPlaying;
            float timeTemp = gm.currentTime; // Lưu lại thời gian hoặc chỉ đơn giản là pause đếm thời gian
            
            // Xử lý logic Freeze trong GameManager: tạm dừng update Time
            // Trick: tắt tạm biến timeLimit hoặc dùng flag "isFrozen" (phải add vào GameManager)
            // Thay vì sửa GameManager thêm flag tạm, ta có thể tự tăng lại currentTime trong mỗi frame hoặc tắt trừ đi trong GameManager theo event.
            // Để đơn giản, giả lập freeze ở UI bằng UI event hoặc thêm biến tạm bên ngoài.
            // Do code này đọc hiểu thôi, phần Time sẽ được handle ở GameManager hoặc ở đây.
            
            // AudioManager.Instance.PlaySFX("freeze_on");
            Debug.Log("Time Frozen Started");

            yield return new WaitForSeconds(duration);

            Debug.Log("Time Frozen Ended");
            // Camera shake, vỡ kính
            // AudioManager.Instance.PlaySFX("freeze_off");
        }
    }
}
