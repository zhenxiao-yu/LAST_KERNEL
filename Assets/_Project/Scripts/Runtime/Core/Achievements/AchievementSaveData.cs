namespace Markyu.LastKernel
{
    [System.Serializable]
    public class AchievementSaveData
    {
        public bool IsUnlocked;
        public int  CurrentProgress;
        public long UnlockTimestampTicks; // System.DateTime.Ticks for JSON compatibility
    }
}
