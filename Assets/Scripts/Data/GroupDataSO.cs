using UnityEngine;

namespace PairPop.Data {
    [CreateAssetMenu(menuName = "PairPop/GroupData", fileName = "NewGroupData")]
    public class GroupDataSO : ScriptableObject {
        public string groupName;
        [Tooltip("Phải có chính xác 4 sprite")]
        public Sprite[] sprites;
        [Tooltip("Màu viền khi cả hàng được ghép (done)")]
        public Color accentColor = Color.white;
    }
}
