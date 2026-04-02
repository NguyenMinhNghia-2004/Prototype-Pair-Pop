using UnityEngine;
using PairPop.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace PairPop.Skills {
    /// <summary>
    /// Hint Booster - Tìm một group active đang có đủ 4 card chưa done,
    /// đổi background 2 card trong group đó thành background done color.
    /// Khi group đó done thì dùng đúng bộ done color này luôn.
    /// </summary>
    [CreateAssetMenu(menuName = "PairPop/Skills/Hint Booster")]
    public class SkillHintBooster : SkillBase {
        [Header("Hint Config")]
        public float highlightDuration = 3f;

        protected override void DoActivate(BoardController board) {
            board.StartCoroutine(HintRoutine(board));
        }

        private IEnumerator HintRoutine(BoardController board) {
            var allCards = board.GetAllCards();
            var activeCards = allCards.Where(c => !c.model.isDone).ToList();
            if (activeCards.Count == 0) yield break;

            // Tìm các group có đủ 4 card chưa done (đủ bài để ghép 1 bộ done)
            var validGroups = activeCards
                .GroupBy(c => c.model.group)
                .Where(g => g.Count() >= 4)
                .ToList();

            if (validGroups.Count == 0) yield break;

            // Chọn random 1 group trong các group hợp lệ
            var chosenGroup = validGroups[Random.Range(0, validGroups.Count)];
            var groupCards = chosenGroup.ToList();

            // Lấy bộ done color tiếp theo sẽ được dùng
            int nextDoneIndex = board.DoneRowCount;
            DoneColorSet colorSet = board.GetDoneColorSet(nextDoneIndex);

            // Chọn 2 card ngẫu nhiên trong group này
            ShuffleList(groupCards);
            var hintCards = groupCards.Take(2).ToList();

            // Ghi nhớ bộ done color đã được "đặt trước" cho group này
            board.ReserveColorForGroup(chosenGroup.Key, colorSet, nextDoneIndex);

            // Đổi background 2 card thành done background
            foreach (var card in hintCards) {
                if (colorSet.backCardSprite != null) {
                    // Lưu sprite gốc trước khi đổi
                    card.SetHintDoneBackground(colorSet.backCardSprite);
                }

                // Hiệu ứng bounce nhẹ
                card.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 4, 0.5f);
            }

            Core.AudioManager.Instance?.PlaySFX("Hint");
            yield return null;
        }

        private void ShuffleList<T>(List<T> list) {
            for (int i = list.Count - 1; i > 0; i--) {
                int j = Random.Range(0, i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
