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
        [Tooltip("Particle effect prefab to spawn on highlighted cards")]
        public GameObject particlePrefab;

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
                
                // Shake animation
                var shakeTween = card.transform.DOShakeRotation(highlightDuration, new Vector3(0, 0, 15f), 10, 90f);
                var scaleTween = card.transform.DOPunchScale(Vector3.one * 0.15f, 0.5f, 2, 0.5f).SetLoops(-1, LoopType.Restart);

                // Spawn practical/particle
                GameObject particle = null;
                if (particlePrefab != null) {
                    particle = Instantiate(particlePrefab, card.transform);
                    particle.transform.localPosition = Vector3.zero;
                }

                DOVirtual.DelayedCall(highlightDuration, () => {
                    if (card != null) {
                        card.HighlightHover(false);
                        shakeTween.Kill();
                        scaleTween.Kill();
                        card.transform.localScale = Vector3.one; // Restore scale
                        card.transform.localRotation = Quaternion.identity; // Restore rotation
                    }
                    if (particle != null) {
                        Destroy(particle);
                    }
                });
            }

            Core.AudioManager.Instance?.PlaySFX("Hint");
            Debug.Log($"[Skill Spotlight] Hinting Group: {groupCounts.Key.groupName}");
        }
    }
}
