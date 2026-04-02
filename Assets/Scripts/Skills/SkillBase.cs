using UnityEngine;
using PairPop.Gameplay;

namespace PairPop.Skills {
    /// <summary>
    /// Base class chung cho tất cả các skill. 
    /// Quản lý: unlock state, usage count, unlock level, UI data.
    /// Các skill cụ thể chỉ cần override DoActivate().
    /// </summary>
    public abstract class SkillBase : ScriptableObject, ISkill {
        [Header("Skill Config")]
        public int unlockLevel = 10;
        public string skillName = "Skill";
        
        [Header("UI Sprites")]
        [Tooltip("Icon lớn hiển thị trên Intro Panel và Refill Panel")]
        public Sprite skillIcon;
        
        [Header("Panel Descriptions")]
        [TextArea, Tooltip("Mô tả cho Intro Panel (ảnh 1) - giới thiệu skill")]
        public string introDescription = "Shows a matching pair";
        [TextArea, Tooltip("Mô tả cho Tutorial Panel (ảnh 2) - hướng dẫn sử dụng")]
        public string tutorialDescription = "Tap the Hint booster to reveal a matching pair!";
        [TextArea, Tooltip("Mô tả cho Refill Panel (ảnh 3) - mua thêm lượt")]
        public string refillDescription = "Shows a pair of cards from the same category.";
        
        [Header("Runtime State (auto-managed)")]
        [SerializeField] private bool isUnlocked = false;
        [SerializeField] private int usageCount = 0;

        // === ISkill Properties ===
        public int UnlockLevel => unlockLevel;
        public string SkillName => skillName;
        public string SkillDescription => introDescription; // backward compat
        
        // Panel descriptions
        public string IntroDescription => introDescription;
        public string TutorialDescription => tutorialDescription;
        public string RefillDescription => refillDescription;
        public Sprite SkillIcon => skillIcon;
        
        public bool IsUnlocked {
            get => isUnlocked;
            set => isUnlocked = value;
        }
        
        public int UsageCount {
            get => usageCount;
            set => usageCount = Mathf.Max(0, value);
        }
        
        public bool CanUse => isUnlocked && usageCount > 0;

        public void AddUsage(int amount) {
            usageCount += amount;
        }

        /// <summary>
        /// Kích hoạt skill: trừ 1 lượt sử dụng rồi gọi logic cụ thể.
        /// </summary>
        public void Activate(BoardController board) {
            if (!CanUse) return;
            usageCount--;
            DoActivate(board);
        }

        /// <summary>
        /// Logic cụ thể của từng skill - override trong subclass.
        /// </summary>
        protected abstract void DoActivate(BoardController board);

        /// <summary>
        /// Reset state khi chơi lại (Replay / New Level).
        /// </summary>
        public void ResetState() {
            isUnlocked = false;
            usageCount = 0;
        }
    }
}
