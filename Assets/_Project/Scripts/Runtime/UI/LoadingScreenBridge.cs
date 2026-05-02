using System.Collections.Generic;
using Michsky.LSS;
using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(LSS_LoadingScreen))]
    public sealed class LoadingScreenBridge : MonoBehaviour
    {
        private static readonly string[] TipKeys =
        {
            "loading.tip.01", "loading.tip.02", "loading.tip.03", "loading.tip.04",
            "loading.tip.05", "loading.tip.06", "loading.tip.07", "loading.tip.08",
            "loading.tip.09", "loading.tip.10", "loading.tip.11", "loading.tip.12",
            "loading.tip.13", "loading.tip.14", "loading.tip.15", "loading.tip.16",
        };

        private void Awake()
        {
            var lss = GetComponent<LSS_LoadingScreen>();
            if (lss == null) return;

            lss.titleObjText    = GameLocalization.Get("menu.title");
            lss.titleObjDescText = GameLocalization.Get("loading.status");

            // Replace LSS's static hint list with localized tips before its Start() runs
            lss.hintList           = BuildLocalizedTips();
            lss.changeHintWithTimer = false;
        }

        private static List<string> BuildLocalizedTips()
        {
            var list = new List<string>(TipKeys.Length);
            foreach (string key in TipKeys)
            {
                string text = GameLocalization.Get(key);
                if (!string.IsNullOrEmpty(text))
                    list.Add(text);
            }
            return list.Count > 0 ? list : new List<string> { "Hold the bunker." };
        }
    }
}
