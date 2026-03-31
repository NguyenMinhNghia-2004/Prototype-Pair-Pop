using UnityEngine;
using DG.Tweening;
using PairPop.Gameplay;
using System.Linq;

namespace PairPop.Skills {
    [CreateAssetMenu(menuName = "PairPop/Skills/Spotlight (Group Hint)")]
    public class SkillSpotlight : ScriptableObject, ISkill {
        public int unlockLevel = 14;
        public int cooldownTurns = 4;
        
        private int lastUsedDoneCount = -999;

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

            var activeCards = board.GetAllCards().Where(c => !c.model.isDone).ToList();
            if (activeCards.Count == 0) return;

            // Tìm group gần done nhất (tức là có nhiều card nhất trên cùng một hàng)
            // Hoặc đơn giản là tìm 1 group bất kỳ random rải rác. Theo doc là group nhiều card nhất trong 1 hàng (nhưng game chia mỗi hàng là 1 random, nên có khi là tìm các lá giống nhau rải trên các hàng).
            var groupCounts = activeCards.GroupBy(c => c.model.group)
                                         .OrderByDescending(g => g.Count())
                                         .FirstOrDefault();
            
            if (groupCounts == null) return;

            var targetGroup = groupCounts.Key;

            foreach (var card in groupCounts) {
                // Glow & Bounce
                card.HighlightHover(true);
                card.transform.DOPunchPosition(Vector3.up * 0.3f, 0.4f, 2, 0.5f).SetLoops(3, LoopType.Restart);

                DOVirtual.DelayedCall(3f, () => {
                    card.HighlightHover(false);
                });
            }

            Debug.Log($"[Skill Spotlight] Hinting Group: {targetGroup.groupName}");
            // Có thể emit Event mở popup name của Group này ở giữa màn hình từ UIManager.
        }
    }
}
