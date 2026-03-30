namespace PairPop.Models {
    [System.Serializable]
    public class CardModel {
        public Data.GroupDataSO group;
        public int spriteIndex;           // 0-3
        public int row, col;              // Vị trí trên board (row 0 thường là trên cùng, hoặc tuỳ logic quản lý)
        public bool isDone;
        public bool isInPool;             // Còn trong pool chưa spawn ra board
        
        // Cập nhật vị trí grid
        public void UpdatePosition(int newRow, int newCol) {
            this.row = newRow;
            this.col = newCol;
        }
    }
}
