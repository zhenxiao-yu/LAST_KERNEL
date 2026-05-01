namespace Markyu.LastKernel.Achievements
{
    public interface IAchievementService
    {
        event System.Action<AchievementDefinition> OnAchievementUnlocked;

        bool IsUnlocked(AchievementDefinition definition);
        float GetProgressNormalized(AchievementDefinition definition);
        int GetProgressCount(AchievementDefinition definition);

        void NotifyCustom(string achievementId);
        void NotifyNightSurvived();
        void NotifyDayReached(int day);
        void NotifyGameWon();
        void NotifyGameLost();
        void NotifyCardDiscovered(string cardId);
    }
}
