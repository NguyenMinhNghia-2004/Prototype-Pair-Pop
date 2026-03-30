using UnityEngine;
using PairPop.Core;
using PairPop.Data;
using PairPop.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace PairPop.Gameplay {
    public class BoardController : MonoBehaviour {
        [Header("Prefabs")]
        public CardController cardPrefab;
        public GameObject ghostCardPrefab;

        [Header("Position Configs")]
        public Vector2 startPoint;
        public Vector2 doneTrayPos;
        public float spacingX = 2.2f;
        public float spacingY = 3.5f;

        // Dữ liệu board hiện tại
        private List<CardController> allCards = new List<CardController>();
        private LevelDataSO currentLevel;
        private Queue<GroupDataSO> spawnPool = new Queue<GroupDataSO>();

        private GameObject currentGhost;
        private CardController hoveredCard;

        private void Start() {
            // Test
            // StartBoard(GameManager.Instance.currentLevel);
        }

        public void StartBoard(LevelDataSO level) {
            currentLevel = level;
            allCards.Clear();
            spawnPool.Clear();

            // Shuffle pool và xếp vào Queue
            var groups = level.possibleGroups.OrderBy(x => Random.value).ToList();
            for (int i = 0; i < level.totalGroupCount; i++) {
                spawnPool.Enqueue(groups[i % groups.Count]);
            }

            // Sinh các hàng bắt đầu (activeGroupCount)
            for (int i = 0; i < level.activeGroupCount; i++) {
                SpawnRow(i);
            }
        }

        private void SpawnRow(int rowIdx) {
            if (spawnPool.Count == 0) return;
            GroupDataSO groupToSpawn = spawnPool.Dequeue();

            List<int> colIndices = new List<int> { 0, 1, 2, 3 };
            colIndices = colIndices.OrderBy(x => Random.value).ToList(); // Shuffle cột

            for (int c = 0; c < 4; c++) {
                int rndCol = colIndices[c];
                Vector3 targetPos = GetCellPosition(rowIdx, rndCol);
                
                CardModel model = new CardModel {
                    group = groupToSpawn,
                    spriteIndex = c,
                    row = rowIdx,
                    col = rndCol,
                    isDone = false,
                    isInPool = false
                };

                CardController cardObj = Instantiate(cardPrefab, new Vector3(8f, -4f, 0), Quaternion.identity, this.transform);
                cardObj.Init(model, this, groupToSpawn.sprites[c]);
                allCards.Add(cardObj);

                // Animate chia bài
                cardObj.transform.DOMove(targetPos, 0.4f).SetDelay((rowIdx * 0.15f) + (c * 0.05f)).SetEase(Ease.OutBack);
                // cardObj.transform.DORotate xáo lật 3d tuỳ ý như prompt
            }
        }

        public Vector3 GetCellPosition(int row, int col) {
            return new Vector3(startPoint.x + col * spacingX, startPoint.y - row * spacingY, 0);
        }

        public bool IsRowInteractable(int row) {
            // Check nếu hàng là hàng đang tương tác (ở trên cùng hoặc dưới cùng tuỳ design, ở đây cho mọi hàng chưa done đều sửa được nếu design cho phép cùng hàng)
            return true; 
        }

        public void ShowGhostCard(Vector3 pos) {
            if (currentGhost == null && ghostCardPrefab != null) {
                currentGhost = Instantiate(ghostCardPrefab, pos, Quaternion.identity, this.transform);
            } else if (currentGhost != null) {
                currentGhost.SetActive(true);
                currentGhost.transform.position = pos;
            }
        }

        public void HideGhostCard() {
            if (currentGhost != null) currentGhost.SetActive(false);
            if (hoveredCard != null) {
                hoveredCard.HighlightHover(false);
                hoveredCard = null;
            }
        }

        public CardController GetCardAtMousePos(Vector3 mousePos) {
            // Find nearby card within radius
            float threshold = 1.5f;
            foreach (var card in allCards) {
                if (Vector3.Distance(card.transform.position, mousePos) < threshold) {
                    return card;
                }
            }
            return null;
        }

        public void CheckHover(CardController draggedCard, Vector3 mousePos) {
            CardController target = GetCardAtMousePos(mousePos);
            if (target != hoveredCard) {
                if (hoveredCard != null) hoveredCard.HighlightHover(false);
                if (target != null && target != draggedCard && target.model.row == draggedCard.model.row) {
                    target.HighlightHover(true);
                }
                hoveredCard = target;
            }
        }

        public void SwapCards(CardController c1, CardController c2) {
            int tempCol = c1.model.col;
            c1.model.col = c2.model.col;
            c2.model.col = tempCol;

            Vector3 pos1 = GetCellPosition(c1.model.row, c1.model.col);
            Vector3 pos2 = GetCellPosition(c2.model.row, c2.model.col);

            c1.transform.DOMove(pos1, 0.2f).SetEase(Ease.InOutQuad);
            c2.transform.DOMove(pos2, 0.2f).SetEase(Ease.InOutQuad);
            c1.transform.DOScale(1f, 0.15f);

            // Check math
            StartCoroutine(CheckRowDoneRoutine(c1.model.row));
        }

        private IEnumerator CheckRowDoneRoutine(int row) {
            yield return new WaitForSeconds(0.25f);
            
            var rowCards = allCards.Where(c => c.model.row == row).ToList();
            if (rowCards.Count != 4) yield break;

            string gName = rowCards[0].model.group.groupName;
            bool isAllMatch = rowCards.All(c => c.model.group.groupName == gName);

            if (isAllMatch) {
                // ROW DONE SEQUENCE
                GameManager.Instance.AddScoreForDoneRow();
                HapticManager.Instance?.Play(HapticType.Success);

                Sequence seq = DOTween.Sequence();
                for (int i = 0; i < 4; i++) {
                    var card = rowCards[i];
                    card.model.isDone = true;
                    // Flash
                    card.FlashBackground(card.model.group.accentColor, 0.2f);
                    // Scale stagger
                    seq.Insert(i * 0.08f, card.transform.DOScale(1.25f, 0.15f).SetLoops(2, LoopType.Yoyo));
                }

                yield return new WaitForSeconds(0.6f);

                // Bay lên khay done
                foreach (var card in rowCards) {
                    card.transform.DOMove(doneTrayPos, 0.35f).SetEase(Ease.InBack);
                    card.transform.DOScale(0.5f, 0.35f);
                }

                // Check Spawn Rules
                CheckAndSpawnNewRow();
            }
        }

        private void CheckAndSpawnNewRow() {
            if (currentLevel.spawnRules == null) return;
            foreach (var rule in currentLevel.spawnRules) {
                if (GameManager.Instance.doneCount == rule.afterDoneCount) {
                    for (int i = 0; i < rule.spawnGroupCount; i++) {
                        // Tìm max row index + 1
                        int maxRow = allCards.Count > 0 ? allCards.Max(c => c.model.row) : 0;
                        SpawnRow(maxRow + 1);
                    }
                }
            }
        }

        public List<CardController> GetAllCards() {
            return allCards;
        }
    }
}
