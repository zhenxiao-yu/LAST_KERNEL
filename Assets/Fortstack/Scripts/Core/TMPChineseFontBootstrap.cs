using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Markyu.LastKernel
{
    internal static class TMPChineseFontBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterChineseFallback()
        {
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
                return;

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            List<TMP_FontAsset> fallbackFonts = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = fallbackFonts;

            defaultFont.isMultiAtlasTexturesEnabled = true;

            foreach (TMP_FontAsset fallbackFont in fallbackFonts)
            {
                if (fallbackFont == null)
                    continue;

                fallbackFont.isMultiAtlasTexturesEnabled = true;

                if (!defaultFont.fallbackFontAssetTable.Contains(fallbackFont))
                {
                    defaultFont.fallbackFontAssetTable.Add(fallbackFont);
                }
            }
        }
    }
}
