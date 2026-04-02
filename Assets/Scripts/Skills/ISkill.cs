namespace PairPop.Skills {
    /// <summary>
    /// Interface chung cho tất cả các skill trong game.
    /// Mỗi skill quản lý: unlock level, số lượt sử dụng, trạng thái unlock.
    /// </summary>
    public interface ISkill {
        /// <summary> Level cần đạt để unlock skill </summary>
        int UnlockLevel { get; }
        
        /// <summary> Tên skill hiển thị trên UI </summary>
        string SkillName { get; }
        
        /// <summary> Mô tả skill hiển thị khi giới thiệu </summary>
        string SkillDescription { get; }
        
        /// <summary> Skill đã được unlock chưa </summary>
        bool IsUnlocked { get; set; }
        
        /// <summary> Số lượt sử dụng còn lại </summary>
        int UsageCount { get; set; }
        
        /// <summary> Có thể sử dụng skill không (unlocked + có lượt) </summary>
        bool CanUse { get; }
        
        /// <summary> Thêm lượt sử dụng </summary>
        void AddUsage(int amount);
        
        /// <summary> Kích hoạt skill - trừ 1 lượt </summary>
        void Activate(Gameplay.BoardController board);
        
        /// <summary> Reset state khi chơi lại </summary>
        void ResetState();
    }
}
