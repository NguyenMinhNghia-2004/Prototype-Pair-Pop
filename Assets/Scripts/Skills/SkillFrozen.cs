using UnityEngine;
using PairPop.Gameplay;
using System.Collections;

namespace PairPop.Skills {
    /// <summary>
    /// Frozen Skill - Tạm dừng thời gian trong khoảng thời gian nhất định.
    /// </summary>
    [CreateAssetMenu(menuName = "PairPop/Skills/Frozen (Time Freeze)")]
    public class SkillFrozen : SkillBase {
        [Header("Frozen Config")]
        public float duration = 20f;

        protected override void DoActivate(BoardController board) {
            board.StartCoroutine(FreezeRoutine());
        }

        private IEnumerator FreezeRoutine() {
            var gm = Core.GameManager.Instance;
            
            // Freeze: tạm dừng trừ thời gian
            gm.isFrozen = true;
            Core.AudioManager.Instance?.PlaySFX("Hint");
            Debug.Log("Time Frozen Started");

            yield return new WaitForSeconds(duration);

            gm.isFrozen = false;
            Debug.Log("Time Frozen Ended");
        }
    }
}
