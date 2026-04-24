using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Markyu.FortStack
{
    internal static class TMPChineseFontBootstrap
    {
        private static readonly string[] WindowsFontCandidates =
        {
            "NotoSansSC-VF.ttf",
            "msyh.ttc",
            "msyhbd.ttc",
            "simhei.ttf",
            "simsunb.ttf",
            "Deng.ttf"
        };

        private static TMP_FontAsset runtimeChineseFallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterChineseFallback()
        {
            if (runtimeChineseFallback != null)
                return;

            if (!IsWindowsPlatform() || TMP_Settings.instance == null)
                return;

            string fontsDirectory = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows),
                "Fonts");

            foreach (string candidateFile in WindowsFontCandidates)
            {
                string candidatePath = Path.Combine(fontsDirectory, candidateFile);
                if (!File.Exists(candidatePath))
                    continue;

                TMP_FontAsset candidateFont = TMP_FontAsset.CreateFontAsset(
                    candidatePath,
                    0,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024);

                if (candidateFont == null)
                    continue;

                candidateFont.name = Path.GetFileNameWithoutExtension(candidateFile) + " TMP Fallback";
                candidateFont.isMultiAtlasTexturesEnabled = true;
                runtimeChineseFallback = candidateFont;
                break;
            }

            if (runtimeChineseFallback == null)
            {
                Debug.LogWarning("TMPChineseFontBootstrap: Unable to create a Chinese TMP fallback from the Windows font directory.");
                return;
            }

            List<TMP_FontAsset> fallbackFonts = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
            if (!fallbackFonts.Contains(runtimeChineseFallback))
            {
                fallbackFonts.Insert(0, runtimeChineseFallback);
                TMP_Settings.fallbackFontAssets = fallbackFonts;
            }
        }

        private static bool IsWindowsPlatform()
        {
            return Application.platform == RuntimePlatform.WindowsEditor
                   || Application.platform == RuntimePlatform.WindowsPlayer;
        }
    }
}
