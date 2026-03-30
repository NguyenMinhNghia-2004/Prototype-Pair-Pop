using PairPop.Gameplay;

namespace PairPop.Skills {
    public interface ISkill {
        int UnlockLevel { get; }
        int CooldownTurns { get; }       // Cooldown tính theo số lần done
        bool IsReady(int currentDoneCount);
        void Activate(BoardController board);
        float CurrentCooldownProgress(int currentDoneCount);
    }
}
