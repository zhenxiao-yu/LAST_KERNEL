namespace Markyu.LastKernel.Achievements
{
    public enum AchievementPlatformType { InGame, Steam, Epic }

    public interface IAchievementPlatform
    {
        AchievementPlatformType PlatformType { get; }
        void Initialize();
        void Unlock(string achievementId);
        void SetProgress(string achievementId, int current, int max);
        bool IsUnlocked(string achievementId);
        void StoreStats();
    }
}
