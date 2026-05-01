// Requires Steamworks.NET: https://github.com/rlabrecque/Steamworks.NET
// Install via Package Manager, then add STEAMWORKS_NET to Project Settings → Player → Scripting Define Symbols.
#if STEAMWORKS_NET
using Steamworks;
#endif
using UnityEngine;

namespace Markyu.LastKernel.Achievements
{
    public class SteamAchievementPlatform : IAchievementPlatform
    {
        public AchievementPlatformType PlatformType => AchievementPlatformType.Steam;

        public void Initialize()
        {
#if !STEAMWORKS_NET
            Debug.LogError("[Achievements] STEAMWORKS_NET scripting define is missing.");
#endif
        }

        public void Unlock(string achievementId)
        {
#if STEAMWORKS_NET
            if (!SteamManager.Initialized) return;
            SteamUserStats.SetAchievement(achievementId);
            SteamUserStats.StoreStats();
#endif
        }

        public void SetProgress(string achievementId, int current, int max)
        {
#if STEAMWORKS_NET
            if (!SteamManager.Initialized) return;
            SteamUserStats.IndicateAchievementProgress(achievementId, (uint)current, (uint)max);
#endif
        }

        public bool IsUnlocked(string achievementId)
        {
#if STEAMWORKS_NET
            if (!SteamManager.Initialized) return false;
            SteamUserStats.GetAchievement(achievementId, out bool achieved);
            return achieved;
#else
            return false;
#endif
        }

        public void StoreStats()
        {
#if STEAMWORKS_NET
            if (SteamManager.Initialized) SteamUserStats.StoreStats();
#endif
        }
    }
}
