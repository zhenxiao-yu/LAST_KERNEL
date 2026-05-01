// Requires Epic Online Services (EOS) SDK for Unity.
// Install via Package Manager, then add EOS_SDK to Project Settings → Player → Scripting Define Symbols.
#if EOS_SDK
using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
#endif
using UnityEngine;

namespace Markyu.LastKernel.Achievements
{
    public class EpicAchievementPlatform : IAchievementPlatform
    {
        public AchievementPlatformType PlatformType => AchievementPlatformType.Epic;

        public void Initialize()
        {
#if !EOS_SDK
            Debug.LogError("[Achievements] EOS_SDK scripting define is missing.");
#endif
        }

        public void Unlock(string achievementId)
        {
#if EOS_SDK
            // Retrieve via EOSManager (project-specific singleton from EOS Unity Plugin).
            // var achievementsInterface = EOSManager.Instance.GetEOSPlatformInterface().GetAchievementsInterface();
            // var options = new UnlockAchievementsOptions { AchievementIds = new Utf8String[] { achievementId } };
            // achievementsInterface.UnlockAchievements(ref options, null, OnUnlockComplete);
#endif
        }

        public void SetProgress(string achievementId, int current, int max)
        {
            // EOS uses stat-based progress. Ingest the relevant stat via StatsInterface instead.
        }

        public bool IsUnlocked(string achievementId) => false; // EOS queries are async; poll separately if needed.

        public void StoreStats() { }
    }
}
