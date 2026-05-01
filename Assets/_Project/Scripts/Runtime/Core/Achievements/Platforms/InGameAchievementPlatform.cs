namespace Markyu.LastKernel.Achievements
{
    // Always-available fallback. State is managed by AchievementService + GameData — no external SDK.
    public class InGameAchievementPlatform : IAchievementPlatform
    {
        public AchievementPlatformType PlatformType => AchievementPlatformType.InGame;

        public void Initialize()  { }
        public void StoreStats()  { }
        public void Unlock(string achievementId) { }
        public void SetProgress(string achievementId, int current, int max) { }
        public bool IsUnlocked(string achievementId) => false; // AchievementService reads GameData directly
    }
}
