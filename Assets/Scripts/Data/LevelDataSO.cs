using UnityEngine;

namespace PairPop.Data {
    [CreateAssetMenu(menuName = "PairPop/LevelData", fileName = "NewLevelData")]
    public class LevelDataSO : ScriptableObject {
        public int columnCount = 4;       // Luôn cố định 4 cột
        public int rowCount;              // Số hàng = số group đang chơi
        public int totalGroupCount;       // Tổng số group trong pool
        public int activeGroupCount;      // Số group hiển thị lúc đầu
        public float timeLimit;           // 0 = không giới hạn
        public SpawnRule[] spawnRules;    // Luật spawn sau khi done
        
        [Tooltip("Danh sách các group có thể xuất hiện trong Level này")]
        public GroupDataSO[] possibleGroups; 
    }

    [System.Serializable]
    public class SpawnRule {
        public int afterDoneCount;        // Spawn sau khi done lần thứ N
        public int spawnGroupCount;       // Số group được spawn thêm
    }
}
