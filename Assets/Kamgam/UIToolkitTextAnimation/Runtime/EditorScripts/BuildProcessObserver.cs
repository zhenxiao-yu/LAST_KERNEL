#if UNITY_EDITOR
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Kamgam.UIToolkitTextAnimation
{
    /// <summary>
    /// Sadly we need this thanks to Unity mindbogglingly bad API design, see
    /// https://discussions.unity.com/t/ipostprocessbuildwithreport-and-qa-embarrasing-answer-about-a-serious-bug/791031/16
    /// </summary>
    public class BuildProcessObserver : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue + 10;

        public static System.Action<BuildReport> OnBuildStarted;
        public static System.Action<BuildReport> OnBuildEnded;

        public void OnPreprocessBuild(BuildReport report)
        {
            OnBuildStarted?.Invoke(report);
            
            // We have to do it this way because IPostprocessBuildWithReport is not fired if the build fails:
            // see: https://discussions.unity.com/t/791031
            waitForBuildCompletion(report);
        }

        async void waitForBuildCompletion(BuildReport report)
        {
            while (BuildPipeline.isBuildingPlayer || report.summary.result == BuildResult.Unknown)
            {
                await Task.Delay(1000);
            }

            OnPostprocessBuild(report);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            OnBuildEnded?.Invoke(report);
        }
    }
}
#endif