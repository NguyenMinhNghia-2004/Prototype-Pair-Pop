using UnityEngine;
using UnityEngine.UI;
using PairPop.Core;
using PairPop.Data;
using PairPop.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace PairPop.Gameplay {

    /// <summary>
    /// Bộ sprite dùng khi done group, gán theo thứ tự done (done 1 = bộ 0, done 2 = bộ 1, ...)
    /// </summary>
    [System.Serializable]
    public class DoneColorSet {
        [Tooltip("Sprite CARD_BASE - thay vào background card khi done")]
        public Sprite backCardSprite;
        [Tooltip("Sprite NICE_SLICE_UI - thay vào BG Box của Done Box")]
        public Sprite bgBoxSprite;
        [Tooltip("Sprite Ribbon - thay vào Box Title của Done Box")]
        public Sprite ribbonSprite;
    }

    public class BoardController : MonoBehaviour {
        [Header("Prefabs")]
        public CardController cardPrefab;
        public GameObject ghostCardPrefab;
        public DoneBoxController doneBoxPrefab;

        [Header("Position Configs")]
        public Vector2 startPoint;
        public Vector2 doneTrayPos;
        public float spacingX = 2.2f;
        public float spacingY = 3.5f;

        [Header("Done Sprite Palette")]
        [Tooltip("Danh sách các bộ sprite (CARD_BASE, NICE_SLICE_UI, Ribbon), dùng theo thứ tự done. Vượt số lượng thì lặp lại.")]
        public DoneColorSet[] doneColorSets;

        // Dữ liệu board hiện tại
        private List<CardController> allCards = new List<CardController>();
        private List<DoneBoxController> doneBoxes = new List<DoneBoxController>();
        private LevelDataSO currentLevel;
        private Queue<GroupDataSO> spawnPool = new Queue<GroupDataSO>();

        private GameObject currentGhost;
        private CardController hoveredCard;
        private Coroutine hideGhostRoutine;
        private int doneRowCount = 0; // Số hàng đã done, dùng để xếp lên trên cùng

        private void Start() {
            GameManager.Instance.StartLevel(GameManager.Instance.currentLevel);
            StartBoard(GameManager.Instance.currentLevel);
        }

        public void StartBoard(LevelDataSO level) {
            currentLevel = level;
            allCards.Clear();
            doneBoxes.Clear();
            spawnPool.Clear();
            doneRowCount = 0;

            var groups = level.possibleGroups.ToList();
            for (int i = 0; i < level.totalGroupCount; i++) {
                spawnPool.Enqueue(groups[i % groups.Count]);
            }

            SpawnRows(0, level.activeGroupCount);
        }

        private void SpawnRows(int startRowIdx, int rowCount) {
            if (spawnPool.Count == 0) return;

            int groupsToSpawnCount = Mathf.Min(rowCount, spawnPool.Count);
            List<CardModel> cardsToSpawn = new List<CardModel>();

            for (int i = 0; i < groupsToSpawnCount; i++) {
                GroupDataSO groupToSpawn = spawnPool.Dequeue();
                for (int c = 0; c < 4; c++) {
                    cardsToSpawn.Add(new CardModel {
                        group = groupToSpawn,
                        spriteIndex = c,
                        isDone = false,
                        isInPool = false
                    });
                }
            }

            ShuffleCardsForSpawn(cardsToSpawn, groupsToSpawnCount);

            int finalMaxRow = Mathf.Max(allCards.Count > 0 ? allCards.Max(c => c.model.row) : 0, startRowIdx + groupsToSpawnCount - 1);

            int cardIndex = 0;
            for (int r = 0; r < groupsToSpawnCount; r++) {
                int rowIdx = startRowIdx + r;
                for (int c = 0; c < 4; c++) {
                    CardModel model = cardsToSpawn[cardIndex++];
                    model.row = rowIdx;
                    model.col = c;
                    
                    Vector2 targetPos = GetCellPosition(rowIdx, c, finalMaxRow);
                    CardController cardObj = Instantiate(cardPrefab, this.transform);
                    RectTransform rt = cardObj.GetComponent<RectTransform>();
                    
                    rt.anchoredPosition = new Vector2(800f, -400f); 
                    
                    cardObj.Init(model, this, model.group.sprites[model.spriteIndex]);
                    allCards.Add(cardObj);

                    rt.DOAnchorPos(targetPos, 0.4f).SetDelay((r * 0.15f) + (c * 0.05f)).SetEase(Ease.OutBack);
                    cardObj.transform.localScale = Vector3.one * 0.7f;
                }
            }
            // If the center shifted, we might want to reposition others, but let's see how it looks.
        }


        private void ShuffleCardsForSpawn(List<CardModel> cards, int rowCount) {
            if (rowCount <= 1) return;
            bool validShuffle = false;
            int maxAttempts = 100;
            int attempts = 0;
            
            while (!validShuffle && attempts < maxAttempts) {
                for (int i = cards.Count - 1; i > 0; i--) {
                    int j = Random.Range(0, i + 1);
                    var temp = cards[i];
                    cards[i] = cards[j];
                    cards[j] = temp;
                }
                
                validShuffle = true;
                for (int r = 0; r < rowCount; r++) {
                    string firstGroupName = cards[r * 4].group.groupName;
                    bool allSame = true;
                    for (int c = 1; c < 4; c++) {
                        if (cards[r * 4 + c].group.groupName != firstGroupName) {
                            allSame = false;
                            break;
                        }
                    }
                    if (allSame) {
                        validShuffle = false;
                        break;
                    }
                }
                attempts++;
            }
        }

        public Vector2 GetCellPosition(int row, int col, int? forcedMaxRow = null) {
            float totalWidth = (4 - 1) * spacingX;
            float startX = startPoint.x - (totalWidth / 2f);

            int maxRow = forcedMaxRow ?? (allCards.Count > 0 ? allCards.Max(c => c.model.row) : 0);
            float totalHeight = maxRow * spacingY;
            float startY = startPoint.y + (totalHeight / 2f);

            return new Vector2(startX + col * spacingX, startY - row * spacingY);
        }

        public bool IsRowInteractable(int row) {
            // Check nếu hàng là hàng đang tương tác (ở trên cùng hoặc dưới cùng tuỳ design, ở đây cho mọi hàng chưa done đều sửa được nếu design cho phép cùng hàng)
            return true; 
        }

        public void ShowGhostCard(Vector3 worldPos) {
            if (hideGhostRoutine != null) {
                StopCoroutine(hideGhostRoutine);
                hideGhostRoutine = null;
            }

            if (currentGhost == null && ghostCardPrefab != null) {
                currentGhost = Instantiate(ghostCardPrefab, this.transform);
            } else if (currentGhost != null) {
                currentGhost.SetActive(true);
            }
            if (currentGhost != null) {
                currentGhost.transform.position = worldPos;
                currentGhost.transform.SetAsFirstSibling();
            }
        }

        public void HideGhostCard() {
            if (currentGhost != null && currentGhost.activeSelf) 
            {
                if (hideGhostRoutine != null) StopCoroutine(hideGhostRoutine);
                hideGhostRoutine = StartCoroutine(HideGhostRoutine());
            }
            if (hoveredCard != null) {
                hoveredCard.HighlightHover(false);
                hoveredCard = null;
            }
        }

        private IEnumerator HideGhostRoutine() {
            yield return new WaitForSeconds(0.3f);
            if (currentGhost != null) currentGhost.SetActive(false);
            hideGhostRoutine = null;
        }

        public CardController HoveredCard => hoveredCard;

        public CardController GetClosestCard(Vector3 worldMousePos, CardController ignoreCard) {
            CardController closest = null;
            float minDistance = float.MaxValue;
            // Tăng bán kính tìm target
            float threshold = 2.0f; 

            foreach (var card in allCards) {
                if (card == ignoreCard) continue;
                if (card.model.isDone) continue;

                // Compare in world space for simplicity as we have worldMousePos
                float dist = Vector3.Distance(card.transform.position, worldMousePos);

                if (dist < minDistance && dist < threshold) {
                    minDistance = dist;
                    closest = card;
                }
            }
            return closest;
        }

        public void CheckHover(CardController draggedCard, Vector3 mousePos) {
            CardController target = GetClosestCard(mousePos, draggedCard);
            if (target != hoveredCard) {
                if (hoveredCard != null) hoveredCard.HighlightHover(false);
                if (target != null) {
                    target.HighlightHover(true);
                }
                hoveredCard = target;
            }
        }

        public void SwapCards(CardController c1, CardController c2) {
            // Đổi cột
            int tempCol = c1.model.col;
            c1.model.col = c2.model.col;
            c2.model.col = tempCol;

            // Đổi hàng
            int tempRow = c1.model.row;
            c1.model.row = c2.model.row;
            c2.model.row = tempRow;

            Vector2 pos1 = GetCellPosition(c1.model.row, c1.model.col);
            Vector2 pos2 = GetCellPosition(c2.model.row, c2.model.col);

            c1.GetComponent<RectTransform>().DOAnchorPos(pos1, 0.25f).SetEase(Ease.OutQuad);
            // Hiệu ứng văng (overshoot) cho thẻ target khi bị hút về
            c2.GetComponent<RectTransform>().DOAnchorPos(pos2, 0.35f).SetEase(Ease.OutBack);
            c1.transform.DOScale(0.7f, 0.15f);
            
            c1.UpdateSorting();
            c2.UpdateSorting();

            // Check match cho cả 2 hàng nếu quá trình đổi liên quan tới 2 hàng khác nhau
            StartCoroutine(CheckRowDoneRoutine(c1.model.row));
            if (c1.model.row != c2.model.row) {
                StartCoroutine(CheckRowDoneRoutine(c2.model.row));
            }
        }

        private IEnumerator CheckRowDoneRoutine(int row) {
            yield return new WaitForSeconds(0.25f);
            
            var rowCards = allCards.Where(c => c.model.row == row).ToList();
            if (rowCards.Count != 4) yield break;

            string gName = rowCards[0].model.group.groupName;
            bool isAllMatch = rowCards.All(c => c.model.group.groupName == gName);

            if (isAllMatch) {
                GroupDataSO doneGroup = rowCards[0].model.group;

                // ROW DONE SEQUENCE
                GameManager.Instance.AddScoreForDoneRow();
                HapticManager.Instance?.Play(HapticType.Success);

                // === Bước 1: Lấy bộ màu theo thứ tự done ===
                DoneColorSet colorSet = GetDoneColorSet(doneRowCount);

                // === Bước 1b: Đổi background color cho từng card ===
                Sequence seq = DOTween.Sequence();
                for (int i = 0; i < 4; i++) {
                    var card = rowCards[i];
                    card.model.isDone = true;
                    
                    // Đổi background sprite theo bộ done
                    card.SetDoneBackground(colorSet.backCardSprite);
                    
                    // Scale stagger hiệu ứng done
                    seq.Insert(i * 0.08f, card.transform.DOScale(0.7f * 1.25f, 0.15f).SetLoops(2, LoopType.Yoyo));
                }

                yield return new WaitForSeconds(0.6f);

                // === Bước 2: Xác định vị trí done row ===
                int targetDoneRow = doneRowCount;
                doneRowCount++;

                // === Bước 3: Đẩy các lá bài chưa done xuống dưới ===
                ReassignNonDoneRows();

                // Tính lại tổng số hàng để căn giữa
                int totalRowsForLayout = CalculateTotalRows();

                // === Bước 4: Thu nhỏ 4 lá bài rồi spawn DoneBox thay thế ===
                Vector2 doneBoxPos = GetDoneBoxPosition(targetDoneRow, totalRowsForLayout);

                // Animate 4 card co lại về trung tâm done box
                for (int i = 0; i < rowCards.Count; i++) {
                    var card = rowCards[i];
                    card.GetComponent<RectTransform>().DOAnchorPos(doneBoxPos, 0.3f)
                        .SetEase(Ease.InBack);
                    card.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
                }

                yield return new WaitForSeconds(0.35f);

                // Xóa 4 card cũ khỏi list và destroy
                foreach (var card in rowCards) {
                    allCards.Remove(card);
                    Destroy(card.gameObject);
                }

                // === Bước 5: Spawn DoneBox prefab ===
                if (doneBoxPrefab != null) {
                    DoneBoxController doneBox = Instantiate(doneBoxPrefab, this.transform);
                    RectTransform boxRT = doneBox.GetComponent<RectTransform>();
                    boxRT.anchoredPosition = doneBoxPos;

                    doneBox.Init(doneGroup, colorSet.backCardSprite, colorSet.bgBoxSprite, colorSet.ribbonSprite);
                    doneBox.PlayAppearAnimation();
                    doneBoxes.Add(doneBox);

                    // Đặt done box ở đúng vị trí sibling (trước các card chưa done)
                    doneBox.transform.SetSiblingIndex(targetDoneRow);
                }

                // === Bước 6: Animate các lá bài chưa done về vị trí mới ===
                var nonDoneCards = allCards.Where(c => !c.model.isDone).ToList();
                foreach (var card in nonDoneCards) {
                    Vector2 newPos = GetCellPosition(card.model.row, card.model.col, totalRowsForLayout);
                    card.GetComponent<RectTransform>().DOAnchorPos(newPos, 0.35f)
                        .SetDelay(0.1f).SetEase(Ease.OutQuad);
                }

                // Reposition existing done boxes too (vì maxRow có thể thay đổi)
                RepositionDoneBoxes(totalRowsForLayout);

                // Check Spawn Rules
                CheckAndSpawnNewRow();
            }
        }

        /// <summary>
        /// Gán lại row cho tất cả lá bài chưa done, bắt đầu từ hàng doneRowCount.
        /// Giữ nguyên thứ tự tương đối (row cũ, col cũ).
        /// </summary>
        private void ReassignNonDoneRows() {
            var nonDoneCards = allCards
                .Where(c => !c.model.isDone)
                .OrderBy(c => c.model.row)
                .ThenBy(c => c.model.col)
                .ToList();

            int currentRow = doneRowCount;
            int currentCol = 0;

            foreach (var card in nonDoneCards) {
                card.model.row = currentRow;
                card.model.col = currentCol;
                currentCol++;
                if (currentCol >= 4) {
                    currentCol = 0;
                    currentRow++;
                }
            }
        }

        /// <summary>
        /// Tính tổng số hàng hiện tại (done boxes + non-done cards)
        /// </summary>
        private int CalculateTotalRows() {
            int maxCardRow = allCards.Count > 0 
                ? allCards.Where(c => !c.model.isDone).Select(c => c.model.row).DefaultIfEmpty(0).Max() 
                : 0;
            return Mathf.Max(maxCardRow, doneRowCount - 1);
        }

        /// <summary>
        /// Lấy vị trí cho DoneBox (căn giữa theo hàng, chiếm toàn bộ chiều ngang)
        /// </summary>
        private Vector2 GetDoneBoxPosition(int doneRow, int totalRows) {
            // DoneBox nằm ở giữa hàng (col trung bình = 1.5 ~ giữa 4 cột)
            return GetCellPosition(doneRow, 1, totalRows) + new Vector2(spacingX * 0.5f, 0f);
        }

        /// <summary>
        /// Cập nhật vị trí tất cả done boxes khi layout thay đổi
        /// </summary>
        private void RepositionDoneBoxes(int totalRows) {
            for (int i = 0; i < doneBoxes.Count; i++) {
                if (doneBoxes[i] == null) continue;
                Vector2 pos = GetDoneBoxPosition(i, totalRows);
                doneBoxes[i].GetComponent<RectTransform>()
                    .DOAnchorPos(pos, 0.35f).SetEase(Ease.OutQuad);
            }
        }

        private void CheckAndSpawnNewRow() {
            if (currentLevel.spawnRules == null) return;
            foreach (var rule in currentLevel.spawnRules) {
                if (GameManager.Instance.doneCount == rule.afterDoneCount) {
                    int maxRow = allCards.Count > 0 ? allCards.Max(c => c.model.row) : -1;
                    SpawnRows(maxRow + 1, rule.spawnGroupCount);
                }
            }
        }

        public List<CardController> GetAllCards() {
            return allCards;
        }

        /// <summary>
        /// Lấy bộ màu theo thứ tự done. Nếu vượt số lượng palette thì lặp lại.
        /// </summary>
        private DoneColorSet GetDoneColorSet(int doneIndex) {
            if (doneColorSets == null || doneColorSets.Length == 0) {
                // Fallback mặc định nếu chưa gán
                return new DoneColorSet();
            }
            return doneColorSets[doneIndex % doneColorSets.Length];
        }
    }
}
