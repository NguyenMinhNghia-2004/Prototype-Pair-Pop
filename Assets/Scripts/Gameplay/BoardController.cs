using UnityEngine;
using UnityEngine.UI;
using PairPop.Core;
using PairPop.Data;
using PairPop.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using PairPop.Skills;
using PairPop.UI;

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
        public GameObject doneParticlePrefab;

        [Header("Position Configs")]
        public Vector2 startPoint;
        public Vector2 doneTrayPos;
        public float spacingX = 2.2f;
        public float spacingY = 3.5f;

        [Header("Card Back")]
        [Tooltip("Sprite mặt sau mặc định nếu GroupDataSO không có")]
        public Sprite defaultCardBackSprite;

        [Header("Done Sprite Palette")]
        [Tooltip("Danh sách các bộ sprite (CARD_BASE, NICE_SLICE_UI, Ribbon), dùng theo thứ tự done. Vượt số lượng thì lặp lại.")]
        public DoneColorSet[] doneColorSets;

        // Dữ liệu board hiện tại
        private List<CardController> allCards = new List<CardController>();
        private List<DoneBoxController> doneBoxes = new List<DoneBoxController>();
        private LevelDataSO currentLevel;

        // === Pool mới: danh sách card đã trộn sẵn ===
        private List<CardModel> cardPool = new List<CardModel>();

        private GameObject currentGhost;
        private CardController hoveredCard;
        private Coroutine hideGhostRoutine;
        private int doneRowCount = 0; // Số hàng đã done, dùng để xếp lên trên cùng

        // === FIX Bug 1: cờ chặn tương tác khi đang animate ===
        private bool _isAnimating = false;
        public bool IsAnimating => _isAnimating;

        // === Hint Booster: reserved color cho group ===
        private Dictionary<GroupDataSO, DoneColorSet> reservedColors = new Dictionary<GroupDataSO, DoneColorSet>();
        private int doneColorIndex = 0; // Index riêng để track done color, chỉ tăng khi dùng color set mới (ko trùng reserve)

        /// <summary> Số hàng đã done - dùng cho skill tính toán </summary>
        public int DoneRowCount => doneRowCount;

        // Đếm số lần spawn tư pool (sau khi done)
        private int spawnRound = 0;

        [Header("Skill Buttons")]
        [Tooltip("Danh sách các SkillButtonUI để gọi TryUnlock khi bắt đầu level")]
        public SkillButtonUI[] skillButtons;

        [Header("Spawn Settings")]
        [Range(0f, 1f)]
        [Tooltip("Tỉ lệ SmartSpawn ưu tiên hoàn thành group (0=hoàn toàn ngẫu nhiên, 1=luôn ưu tiên)")]
        public float assistRate = 0.7f;

        private void Start() {
            GameManager.Instance.StartLevel(GameManager.Instance.currentLevel);
            StartBoard(GameManager.Instance.currentLevel);
        }

        public void StartBoard(LevelDataSO level) {
            currentLevel = level;
            allCards.Clear();
            doneBoxes.Clear();
            cardPool.Clear();
            reservedColors.Clear();
            doneRowCount = 0;
            doneColorIndex = 0;
            spawnRound = 0;

            // Tạo pool thuần ngẫu nhiên - shuffle toàn bộ
            BuildShuffledPool(level);

            // Spawn lượng card ban đầu = activeGroupCount * 4
            int initialCardCount = level.activeGroupCount * 4;
            SpawnCardsFromPool(initialCardCount);

            // Chờ chia bài xong rồi mới check unlock skill → hiện intro panel (xuất hiện sớm hơn một chút)
            float spawnAnimDuration = Mathf.Max(0f, CalculateSpawnAnimDuration(level.activeGroupCount) - 0.5f);
            StartCoroutine(TryUnlockSkillsDelayed(spawnAnimDuration));
        }

        /// <summary>
        /// Build pool phẳng: tất cả group (totalGroupCount), mỗi group đủ 4 lá,
        /// sau đó shuffle ngẫu nhiên toàn bộ. SmartSpawn sẽ chịu trách nhiệm
        /// phân phối thông minh khi spawn.
        /// </summary>
        private void BuildShuffledPool(LevelDataSO level) {
            cardPool.Clear();

            var groups = level.possibleGroups
                .Take(level.totalGroupCount)
                .ToList();

            foreach (var g in groups) {
                for (int i = 0; i < 4; i++) {
                    cardPool.Add(new CardModel {
                        group = g,
                        spriteIndex = i,
                        isDone = false,
                        isInPool = true
                    });
                }
            }

            ShuffleList(cardPool);
        }

        // =====================================================================
        // BOARD & POOL ANALYZER
        // =====================================================================

        /// <summary>Đếm số lá mỗi group đang ACTIVE trên board (chưa done).</summary>
        private Dictionary<string, int> AnalyzeBoardGroups() {
            return allCards
                .Where(c => !c.model.isDone)
                .GroupBy(c => c.model.group.groupName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>Đếm số lá mỗi group còn trong pool tạm.</summary>
        private Dictionary<string, int> AnalyzePoolGroups(List<CardModel> pool) {
            return pool
                .GroupBy(c => c.group.groupName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        // =====================================================================
        // SMART SPAWN (CORE)
        // =====================================================================

        /// <summary>
        /// Thuật toán spawn thông minh 3 bước:
        ///   STEP 1 (assistRate): Ưu tiên hoàn thành group đã có trên board
        ///   STEP 2: Tạo group mới hoàn chỉnh từ pool
        ///   STEP 3: Fill còn lại ngẫu nhiên, giới hạn tối đa 2 lá cùng group
        /// </summary>
        private List<CardModel> SmartSpawn(int count) {
            List<CardModel> result = new List<CardModel>();
            List<CardModel> tempPool = new List<CardModel>(cardPool);

            if (tempPool.Count == 0) return result;

            var poolGroups = AnalyzePoolGroups(tempPool);

            // === ANTI-COLLAPSE: pool chỉ còn 1 group → lấy hết không filter ===
            if (poolGroups.Count == 1) {
                var forced = tempPool.Take(Mathf.Min(count, tempPool.Count)).ToList();
                foreach (var c in forced) cardPool.Remove(c);
                return forced;
            }

            // === STEP 1: Ưu tiên hoàn thành group đang có trên board ===
            if (Random.value < assistRate) {
                var boardGroups = AnalyzeBoardGroups();
                foreach (var kv in boardGroups.OrderByDescending(x => x.Value)) {
                    string groupName = kv.Key;
                    int boardCount = kv.Value;

                    if (boardCount >= 4) continue; // Đã đủ rồi, bỏ qua

                    int needed = 4 - boardCount;
                    var candidates = tempPool
                        .Where(c => c.group.groupName == groupName)
                        .Take(needed)
                        .ToList();

                    if (candidates.Count == needed) {
                        result.AddRange(candidates);
                        foreach (var c in candidates) tempPool.Remove(c);

                        if (result.Count >= count)
                            return FinalizeSpawn(result, count);
                    }
                }
            }

            // === STEP 2: Tạo group mới hoàn chỉnh (đủ 4 lá) ===
            var availableGroups = tempPool
                .GroupBy(c => c.group.groupName)
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var g in availableGroups) {
                if (g.Count() < 4) continue;

                var fullSet = g.Take(4).ToList();
                result.AddRange(fullSet);
                foreach (var c in fullSet) tempPool.Remove(c);

                if (result.Count >= count)
                    return FinalizeSpawn(result, count);
            }

            // === STEP 3: Fill còn lại ngẫu nhiên, anti-cluster (tối đa 2 lá/group) ===
            int safe = 0;
            while (result.Count < count && tempPool.Count > 0 && safe < 200) {
                safe++;
                var pick = tempPool[Random.Range(0, tempPool.Count)];
                int sameGroup = result.Count(c => c.group == pick.group);
                if (sameGroup >= 2) continue;
                result.Add(pick);
                tempPool.Remove(pick);
            }

            return FinalizeSpawn(result, count);
        }

        /// <summary>Cắt kết quả đến đúng count, xóa khỏi pool thực, shuffle.</summary>
        private List<CardModel> FinalizeSpawn(List<CardModel> result, int count) {
            if (result.Count > count)
                result = result.Take(count).ToList();

            foreach (var c in result)
                cardPool.Remove(c);

            ShuffleList(result);
            return result;
        }

        /// <summary>
        /// Spawn N lá card từ pool (thay vì spawn theo group).
        /// Card sẽ được chia vào các hàng (4 card/hàng).
        /// </summary>
        private void SpawnCardsFromPool(int cardCount, bool fromDoneBox = false, Vector2? doneBoxOrigin = null, int? forceRow = null, bool useSmartSpawn = false) {
            if (cardPool.Count == 0) return;

            int actualCount = Mathf.Min(cardCount, cardPool.Count);
            List<CardModel> cardsToSpawn;

            if (useSmartSpawn) {
                // Spawn sau done: SmartSpawn phân phối thông minh theo board state
                cardsToSpawn = SmartSpawn(actualCount);
            } else {
                // Spawn ban đầu: lấy lần lượt từ pool (đã shuffle sẵn)
                cardsToSpawn = new List<CardModel>();
                for (int i = 0; i < actualCount; i++) {
                    CardModel card = cardPool[0];
                    cardPool.RemoveAt(0);
                    card.isInPool = false;
                    cardsToSpawn.Add(card);
                }
            }

            // Tính row bắt đầu
            int startRowIdx = forceRow ?? doneRowCount;
            if (!forceRow.HasValue && allCards.Count > 0) {
                var nonDoneCards = allCards.Where(c => !c.model.isDone);
                if (nonDoneCards.Any()) {
                    startRowIdx = nonDoneCards.Max(c => c.model.row) + 1;
                }
            }

            int rowCount = Mathf.CeilToInt((float)actualCount / 4);

            // Shuffle để đảm bảo không cùng hàng cùng group
            ShuffleCardsForSpawn(cardsToSpawn, rowCount);

            int totalRowsForLayout = CalculateTotalRowsWithExtra(startRowIdx + rowCount - 1);

            AudioManager.Instance.PlaySFX("Shuffle");

            int cardIndex = 0;
            for (int r = 0; r < rowCount; r++) {
                int rowIdx = startRowIdx + r;
                for (int c = 0; c < 4 && cardIndex < actualCount; c++) {
                    CardModel model = cardsToSpawn[cardIndex++];
                    model.row = rowIdx;
                    model.col = c;

                    Vector2 targetPos = GetCellPosition(rowIdx, c, totalRowsForLayout);
                    CardController cardObj = Instantiate(cardPrefab, this.transform);
                    RectTransform rt = cardObj.GetComponent<RectTransform>();

                    if (fromDoneBox && doneBoxOrigin.HasValue) {
                        // Card bay ra từ done box
                        rt.anchoredPosition = doneBoxOrigin.Value;
                        cardObj.transform.localScale = Vector3.zero;
                        
                        // Hiện mặt sau trước
                        Sprite backSprite = model.group.cardBackSprite != null 
                            ? model.group.cardBackSprite 
                            : defaultCardBackSprite;
                        
                        cardObj.Init(model, this, model.group.sprites[model.spriteIndex]);
                        cardObj.ShowBack();
                        if (backSprite != null) cardObj.SetCardBackSprite(backSprite);
                        
                        allCards.Add(cardObj);
                        
                        // Animation sequence: nảy ra → di chuyển đến vị trí → lật mặt trước
                        float delay = (r * 0.15f) + (c * 0.05f);
                        Sequence cardSeq = DOTween.Sequence();
                        cardSeq.SetDelay(delay);
                        
                        // Scale lên + bay ra vị trí
                        cardSeq.Append(rt.DOAnchorPos(targetPos, 0.4f).SetEase(Ease.OutBack));
                        cardSeq.Join(cardObj.transform.DOScale(0.7f, 0.35f).SetEase(Ease.OutBack));
                        
                        // Pause ngắn rồi lật mặt trước
                        cardSeq.AppendInterval(0.2f);
                        cardSeq.AppendCallback(() => {
                            cardObj.FlipToFront(0.3f);
                        });
                    } else {
                        // Spawn bình thường: bay từ ngoài màn hình
                        rt.anchoredPosition = new Vector2(800f, -400f);
                        
                        cardObj.Init(model, this, model.group.sprites[model.spriteIndex]);
                        allCards.Add(cardObj);
                        
                        rt.DOAnchorPos(targetPos, 0.4f).SetDelay((r * 0.15f) + (c * 0.05f)).SetEase(Ease.OutBack);
                        cardObj.transform.localScale = Vector3.one * 0.7f;
                    }
                }
            }

            // Reposition existing non-done cards and done boxes
            RepositionDoneBoxes(totalRowsForLayout);
        }

        /// <summary>
        /// Tính tổng rows bao gồm cả rows sẽ spawn thêm
        /// </summary>
        private int CalculateTotalRowsWithExtra(int maxRow) {
            int maxCardRow = allCards.Count > 0
                ? allCards.Where(c => !c.model.isDone).Select(c => c.model.row).DefaultIfEmpty(0).Max()
                : 0;
            return Mathf.Max(maxCardRow, maxRow, doneRowCount - 1);
        }

        /// <summary>
        /// Tính thời gian animation chia bài dựa trên số group
        /// </summary>
        private float CalculateSpawnAnimDuration(int groupCount) {
            // Card cuối cùng có delay = (groupCount-1)*0.15 + 3*0.05 + animation 0.4
            // Thêm buffer 0.3s 
            return (groupCount - 1) * 0.15f + 3 * 0.05f + 0.4f + 0.3f;
        }

        /// <summary>
        /// Chờ chia bài xong → kiểm tra unlock skill
        /// </summary>
        private IEnumerator TryUnlockSkillsDelayed(float delay) {
            yield return new WaitForSeconds(delay);
            TryUnlockSkills();
        }

        /// <summary>
        /// Kiểm tra và unlock các skill theo level hiện tại
        /// </summary>
        private void TryUnlockSkills() {
            if (skillButtons == null) return;
            int currentLevelIdx = GameManager.currentLevelIndex + 1; // 1-based level
            foreach (var btn in skillButtons) {
                if (btn != null) {
                    btn.TryUnlock(currentLevelIdx);
                }
            }
        }

        /// <summary>
        /// Shuffle và kiểm tra không có hàng nào gồm 4 lá cùng group.
        /// Hoạt động với cả trường hợp chỉ có 1 hàng (spawn 4 lá cùng lúc).
        /// </summary>
        private void ShuffleCardsForSpawn(List<CardModel> cards, int rowCount) {
            // Phàn trước chỉ skip khi rowCount<=1 nhưng điều đó khiến 4 lá cùng group vẫn có thể lọt qua
            // Luôn luôn kiểm tra điều kiện không cùng group trước khi trả về
            bool validShuffle = false;
            int maxAttempts = 200;
            int attempts = 0;
            
            while (!validShuffle && attempts < maxAttempts) {
                ShuffleList(cards);
                
                validShuffle = true;
                for (int r = 0; r < rowCount; r++) {
                    int startIdx = r * 4;
                    // Lấy các card trong hàng này (tối đa 4 lá)
                    int endIdx = Mathf.Min(startIdx + 4, cards.Count);
                    int inRow = endIdx - startIdx;
                    if (inRow < 2) break; // Không đủ lá để kiểm tra
                    
                    string firstGroupName = cards[startIdx].group.groupName;
                    bool allSame = true;
                    for (int c = 1; c < inRow; c++) {
                        if (cards[startIdx + c].group.groupName != firstGroupName) {
                            allSame = false;
                            break;
                        }
                    }
                    // Không cho phép: hàng có >=3 lá mà tất cả cùng group
                    if (allSame && inRow >= 3) {
                        validShuffle = false;
                        break;
                    }
                }
                attempts++;
            }
        }

        /// <summary>
        /// Fisher-Yates shuffle
        /// </summary>
        private void ShuffleList<T>(List<T> list) {
            for (int i = list.Count - 1; i > 0; i--) {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
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
            yield return new WaitForSeconds(0.2f);
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

            c1.GetComponent<RectTransform>().DOAnchorPos(pos1, 0.2f).SetEase(Ease.OutQuad);
            // Hiệu ứng văng (overshoot) cho thẻ target khi bị hút về
            c2.GetComponent<RectTransform>().DOAnchorPos(pos2, 0.35f).SetEase(Ease.OutBack);
            c1.transform.DOScale(0.7f, 0.15f);
            
            c1.UpdateSorting();
            c2.UpdateSorting();

            // Xử lý tuần tự các match để không bị lỗi đụng độ layout (Race Condition) khi 2 match cùng lúc
            StartCoroutine(ProcessSwapMatches(c1, c2));
        }

        private IEnumerator ProcessSwapMatches(CardController c1, CardController c2) {
            // Chờ hiệu ứng swap visual bay xong
            yield return new WaitForSeconds(0.2f);

            bool match1 = IsRowMatch(c1.model.row);
            if (match1) {
                yield return StartCoroutine(CheckRowDoneRoutine(c1.model.row, true));
            }

            // Sau khi xử lý xong match1, layout có thể đã bị đẩy xuống, row của c2 được cập nhật tự động!
            if (!c2.model.isDone) {
                bool match2 = IsRowMatch(c2.model.row);
                if (match2) {
                    yield return StartCoroutine(CheckRowDoneRoutine(c2.model.row, true));
                }
            }
        }

        private bool IsRowMatch(int row) {
            var rowCards = allCards.Where(c => c.model.row == row && !c.model.isDone).ToList();
            if (rowCards.Count != 4) return false;
            string gName = rowCards[0].model.group.groupName;
            return rowCards.All(c => c.model.group.groupName == gName);
        }

        // Chống double sound: ghi lại thời điểm phát "Match" cuối cùng
        private float lastMatchSoundTime = -1f;

        private IEnumerator CheckRowDoneRoutine(int row, bool skipInitialDelay = false) {
            if (!skipInitialDelay) {
                yield return new WaitForSeconds(0.2f);
            }
            
            var rowCards = allCards.Where(c => c.model.row == row && !c.model.isDone).ToList();
            if (rowCards.Count != 4) yield break;

            string gName = rowCards[0].model.group.groupName;
            bool isAllMatch = rowCards.All(c => c.model.group.groupName == gName);

            if (isAllMatch) {
                // === FIX Bug 1: Bật cờ animating để block input ===
                _isAnimating = true;

                GroupDataSO doneGroup = rowCards[0].model.group;

                // ROW DONE SEQUENCE
                GameManager.Instance.AddScoreForDoneRow();
                int capturedDoneCount = GameManager.Instance.doneCount; // Capture doneCount immediately to avoid race condition!

                HapticManager.Instance?.Play(HapticType.Heavy);
                yield return new WaitForSeconds(0.05f);
                HapticManager.Instance?.Play(HapticType.Medium);
                
                // === Bước 1: Lấy bộ màu - ưu tiên reserved color (từ Hint Booster) ===
                DoneColorSet colorSet;
                if (reservedColors.TryGetValue(doneGroup, out DoneColorSet reserved)) {
                    colorSet = reserved;
                    reservedColors.Remove(doneGroup);
                } else {
                    colorSet = GetNextDoneColorSet();
                }

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

                yield return new WaitForSeconds(0.2f);

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
                    card.GetComponent<RectTransform>().DOAnchorPos(doneBoxPos, 0.2f)
                        .SetEase(Ease.InBack);
                    card.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
                }

                yield return new WaitForSeconds(0.2f);

                // Xóa 4 card cũ khỏi list và destroy
                foreach (var card in rowCards) {
                    allCards.Remove(card);
                    Destroy(card.gameObject);
                }

                // === FIX: Chỉ phát "Match" sound 1 lần nếu 2 đôi done cùng lúc ===
                if (Time.time - lastMatchSoundTime > 0.5f) {
                    AudioManager.Instance.PlaySFX("Match");
                    lastMatchSoundTime = Time.time;
                }
                

                // === Bước 5: Spawn DoneBox prefab ===
                DoneBoxController spawnedDoneBox = null;
                if (doneBoxPrefab != null) {
                    DoneBoxController doneBox = Instantiate(doneBoxPrefab, this.transform);
                    RectTransform boxRT = doneBox.GetComponent<RectTransform>();
                    boxRT.anchoredPosition = doneBoxPos;

                    doneBox.Init(doneGroup, colorSet.backCardSprite, colorSet.bgBoxSprite, colorSet.ribbonSprite);
                    doneBox.PlayAppearAnimation();
                    doneBoxes.Add(doneBox);
                    spawnedDoneBox = doneBox;

                    // Đặt done box ở đúng vị trí sibling (trước các card chưa done)
                    doneBox.transform.SetSiblingIndex(targetDoneRow);
                    HapticManager.Instance.Play(HapticType.Light);
                    if (doneParticlePrefab != null) {
                        GameObject particle = Instantiate(doneParticlePrefab, this.transform);
                        RectTransform particleRT = particle.GetComponent<RectTransform>();
                        if (particleRT != null) {
                            particleRT.anchoredPosition = doneBoxPos;
                        } else {
                            particle.transform.position = doneBox.transform.position;
                        }
                        Destroy(particle, 4f);
                    }
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

                // Chờ animation chuyển hàng xong
                yield return new WaitForSeconds(0.2f);

                // === FIX Bug 3: Check spawn rules - clear done box và nảy card ra ===
                yield return StartCoroutine(CheckAndSpawnFromDoneBox(spawnedDoneBox, doneBoxPos, targetDoneRow, doneGroup, colorSet, capturedDoneCount));

                // === FIX Bug 1: Tắt cờ animating ===
                _isAnimating = false;
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

        /// <summary>
        /// === FIX Bug 3: Logic spawn sau done ===
        /// Khi kết thúc animation done box:
        /// 1. Ẩn done box đi và xóa khỏi list (clear các lá bài box cũ)
        /// 2. Nảy ra 4 card ngay tại box
        /// 3. Gom xếp bài lại vào phía lá bài ngoài cùng bên phải của hàng đó
        /// 4. Clear 4 lá bài cũ
        /// 5. Spawn 4 lá mới từ pool vào đúng hàng đó mà không bị lỗi bố cục
        /// </summary>
        private IEnumerator CheckAndSpawnFromDoneBox(DoneBoxController doneBox, Vector2 doneBoxPos, int doneRow, GroupDataSO doneGroup, DoneColorSet colorSet, int capturedDoneCount) {
            if (currentLevel.spawnRules == null) yield break;

            int cardsToSpawn = 0;

            // Kiểm tra spawn rule bằng capturedDoneCount thay vì GameManager.Instance.doneCount
            foreach (var rule in currentLevel.spawnRules) {
                if (capturedDoneCount == rule.afterDoneCount) {
                    cardsToSpawn = rule.spawnCardCount;
                    break;
                }
            }

            if (cardsToSpawn <= 0 || cardPool.Count == 0) yield break;

            // Chờ done box animation xong
            yield return new WaitForSeconds(0.2f);

            // === Bước 1: Ẩn done box và loại bỏ để lấy chỗ cho bài mới ===
            if (doneBox != null) {
                doneBox.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
                yield return new WaitForSeconds(0.2f);
                doneBox.gameObject.SetActive(false);
                
                doneBoxes.Remove(doneBox);
                Destroy(doneBox.gameObject);
                doneRowCount--; // clear xong thì doneRowCount giảm đi 1 
            }

            // === Bước 2: Nảy ra 4 card trong box ===
            List<CardController> oldCards = new List<CardController>();
            for (int i = 0; i < 4; i++) {
                CardModel fakeModel = new CardModel { group = doneGroup, spriteIndex = i, isDone = true };
                CardController fakeCard = Instantiate(cardPrefab, this.transform);
                fakeCard.Init(fakeModel, this, doneGroup.sprites[i]);
                fakeCard.SetDoneBackground(colorSet.backCardSprite);
                
                RectTransform rt = fakeCard.GetComponent<RectTransform>();
                rt.anchoredPosition = doneBoxPos;
                fakeCard.transform.localScale = Vector3.zero;
                oldCards.Add(fakeCard);

                Vector2 cellPos = GetCellPosition(doneRow, i, CalculateTotalRows()); 
                fakeCard.transform.DOScale(0.7f, 0.3f).SetEase(Ease.OutBack).SetDelay(i * 0.05f);
                rt.DOAnchorPos(cellPos, 0.4f).SetEase(Ease.OutBack).SetDelay(i * 0.05f);
            }
            HapticManager.Instance.Play(HapticType.Light);
            yield return new WaitForSeconds(0.2f); // Chờ bay ra xong

            // === Bước 3: Gom xếp bài lại vào phía lá bài ngoài cùng bên phải của hàng đó ===
            Vector2 rightmostPos = GetCellPosition(doneRow, 3, CalculateTotalRows());
            for (int i = 0; i < 4; i++) {
                RectTransform rt = oldCards[i].GetComponent<RectTransform>();
                rt.DOAnchorPos(rightmostPos, 0.3f).SetEase(Ease.InBack).SetDelay(i * 0.05f);
                oldCards[i].transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).SetDelay(i * 0.05f);
            }

            yield return new WaitForSeconds(0.3f);

            // === Bước 4: Clear các lá bài box cũ ===
            foreach (var card in oldCards) {
                Destroy(card.gameObject);
            }

            // === Bước 5: Spawn lá mới bằng SmartSpawn ===
            // SmartSpawn tự phân tích board + pool để quyết định:
            //   - Hoàn thành group đang thiếu trên board
            //   - Tạo group mới hoàn chỉnh
            //   - Fill ngẫu nhiên anti-cluster
            SpawnCardsFromPool(cardsToSpawn, fromDoneBox: false, forceRow: doneRow, useSmartSpawn: true);

            // Re-assign row indices safely in case card spans multiple rows
            ReassignNonDoneRows();
            int totalRows = CalculateTotalRows();

            // Animate các lá bài chưa done (bao gồm cả lá bài mới spawn) về đúng layout
            var nonDoneCards = allCards.Where(c => !c.model.isDone).ToList();
            foreach (var card in nonDoneCards) {
                Vector2 newPos = GetCellPosition(card.model.row, card.model.col, totalRows);
                card.GetComponent<RectTransform>().DOAnchorPos(newPos, 0.35f)
                    .SetDelay(0.1f).SetEase(Ease.OutQuad);
            }
            
            RepositionDoneBoxes(totalRows);
            yield return new WaitForSeconds(0.2f);
        }

        public List<CardController> GetAllCards() {
            return allCards;
        }

        /// <summary>
        /// Lấy bộ màu theo index. Nếu vượt số lượng palette thì lặp lại từ đầu.
        /// Public để skill có thể truy cập.
        /// </summary>
        public DoneColorSet GetDoneColorSet(int doneIndex) {
            if (doneColorSets == null || doneColorSets.Length == 0) {
                return new DoneColorSet();
            }
            return doneColorSets[doneIndex % doneColorSets.Length];
        }

        /// <summary>
        /// Lấy bộ màu tiếp theo theo thứ tự, tự động cycling khi hết palette.
        /// </summary>
        private DoneColorSet GetNextDoneColorSet() {
            if (doneColorSets == null || doneColorSets.Length == 0) {
                return new DoneColorSet();
            }
            DoneColorSet set = doneColorSets[doneColorIndex % doneColorSets.Length];
            doneColorIndex++;
            return set;
        }

        /// <summary>
        /// Hint Booster: đặt trước bộ done color cho một group cụ thể.
        /// Khi group đó done, sẽ dùng đúng bộ color này thay vì lấy theo thứ tự.
        /// </summary>
        public void ReserveColorForGroup(GroupDataSO group, DoneColorSet colorSet, int usedIndex) {
            reservedColors[group] = colorSet;
            // Tăng doneColorIndex để tránh trùng với lần done tiếp theo
            if (usedIndex >= doneColorIndex) {
                doneColorIndex = usedIndex + 1;
            }
        }
    }
}
