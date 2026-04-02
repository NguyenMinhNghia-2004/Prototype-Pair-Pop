using UnityEngine;
using DG.Tweening;
using PairPop.Gameplay;
using System.Linq;

namespace PairPop.Skills {
    /// <summary>
    /// Spotlight Skill - Highlight tất cả card của 1 group gần done nhất.
    /// </summary>
    [CreateAssetMenu(menuName = "PairPop/Skills/Spotlight (Group Hint)")]
    public class SkillSpotlight : SkillBase {
        [Header("Spotlight Config")]
        public float highlightDuration = 3f;

        protected override void DoActivate(BoardController board) {
            var activeCards = board.GetAllCards().Where(c => !c.model.isDone).ToList();
            if (activeCards.Count == 0) return;

            // Tìm group có nhiều card nhất (gần done nhất)
            var groupCounts = activeCards.GroupBy(c => c.model.group)
                                         .OrderByDescending(g => g.Count())
                                         .FirstOrDefault();
            
            if (groupCounts == null) return;

            foreach (var card in groupCounts) {
                card.HighlightHover(true);
                card.transform.DOPunchPosition(Vector3.up * 0.3f, 0.4f, 2, 0.5f)
                    .SetLoops(3, LoopType.Restart);

                DOVirtual.DelayedCall(highlightDuration, () => {
                    card.HighlightHover(false);
                });
            }

            Core.AudioManager.Instance?.PlaySFX("Hint");
            Debug.Log($"[Skill Spotlight] Hinting Group: {groupCounts.Key.groupName}");
        }
    }
}
