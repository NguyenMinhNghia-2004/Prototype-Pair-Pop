// using UnityEngine;
// using DG.Tweening;
// using PairPop.Gameplay;
// using System.Linq;

// namespace PairPop.Skills {
//     [CreateAssetMenu(menuName = "PairPop/Skills/Reveal (Search Card)")]
//     public class SkillReveal : ScriptableObject, ISkill {
//         public int unlockLevel = 10;
//         public int cooldownTurns = 3;
        
//         private int lastUsedDoneCount = -999;

//         public int UnlockLevel => unlockLevel;
//         public int CooldownTurns => cooldownTurns;

//         public bool IsReady(int currentDoneCount) {
//             return (currentDoneCount - lastUsedDoneCount) >= cooldownTurns;
//         }

//         public float CurrentCooldownProgress(int currentDoneCount) {
//             float diff = currentDoneCount - lastUsedDoneCount;
//             return Mathf.Clamp01(diff / cooldownTurns);
//         }

//         public void Activate(BoardController board) {
//             lastUsedDoneCount = Core.GameManager.Instance.doneCount;
            
//             var cards = board.GetAllCards().Where(c => !c.model.isDone).ToList();

//             foreach (var card in cards) {
//                 // Background màu vàng nhạt pulse
//                 card.FlashBackground(new Color(1f, 0.9f, 0.5f, 1f), 2.5f);

//                 // Group Color Coding - giả lập một màu tint để dễ nhìn theo group
//                 card.borderGlow.DOColor(card.model.group.accentColor, 0.1f);
//                 card.HighlightHover(true);

//                 // Scale Bounce
//                 Sequence seq = DOTween.Sequence();
//                 seq.Append(card.transform.DOScale(1.2f, 0.15f))
//                    .Append(card.transform.DOScale(0.9f, 0.15f))
//                    .Append(card.transform.DOScale(1.0f, 0.1f))
//                    .SetDelay(Random.Range(0f, 0.2f));

//                 Core.ParticleManager.Instance.PlayMagicSparkle(card.transform.position);

//                 DOVirtual.DelayedCall(2.5f, () => {
//                     card.HighlightHover(false);
//                     card.borderGlow.color = new Color(1, 1, 1, 0);
//                     card.backgroundRenderer.color = Color.white; // Trả về màu nền
//                 });
//             }
//         }
//     }
// }
