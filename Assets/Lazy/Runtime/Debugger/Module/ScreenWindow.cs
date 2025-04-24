using System;
using UnityEngine;

namespace Lazy
{
    [Serializable]
    public class ScreenWindow : ScrollableDebuggerWindowBase
    {
        protected override void OnBeforeDrawScroll()
        {
            base.OnBeforeDrawScroll();
            GUILayout.Label("<b>Screen information</b>");
        }

        protected override void OnDrawScrollableWindow()
        {
            GUILayout.BeginVertical("box");
            {
                DrawTagLabel("Current Resolution", GetResolutionString(Screen.currentResolution));
                DrawTagLabel(
                    "Screen Width",
                    TextUtility.Format(
                        "{0} px / {1} in / {2} cm",
                        Screen.width.ToString(),
                        ScreenConvertUtility.GetInchesFromPixels(Screen.width).ToString("F2"),
                        ScreenConvertUtility.GetCentimetersFromPixels(Screen.width).ToString("F2")
                    )
                );
                DrawTagLabel(
                    "Screen Height",
                    TextUtility.Format(
                        "{0} px / {1} in / {2} cm",
                        Screen.height.ToString(),
                        ScreenConvertUtility.GetInchesFromPixels(Screen.height).ToString("F2"),
                        ScreenConvertUtility.GetCentimetersFromPixels(Screen.height).ToString("F2")
                    )
                );
                DrawTagLabel("Screen DPI", Screen.dpi.ToString("F2"));
                DrawTagLabel("Screen Orientation", Screen.orientation.ToString());
                DrawTagLabel("Is Full Screen", Screen.fullScreen.ToString());
#if UNITY_2018_1_OR_NEWER
                DrawTagLabel("Full Screen Mode", Screen.fullScreenMode.ToString());
#endif
                DrawTagLabel("Sleep Timeout", GetSleepTimeoutDescription(Screen.sleepTimeout));
#if UNITY_2019_2_OR_NEWER
                DrawTagLabel("Brightness", Screen.brightness.ToString("F2"));
#endif
                DrawTagLabel("Cursor Visible", Cursor.visible.ToString());
                DrawTagLabel("Cursor Lock State", Cursor.lockState.ToString());
                DrawTagLabel("Auto Landscape Left", Screen.autorotateToLandscapeLeft.ToString());
                DrawTagLabel("Auto Landscape Right", Screen.autorotateToLandscapeRight.ToString());
                DrawTagLabel("Auto Portrait", Screen.autorotateToPortrait.ToString());
                DrawTagLabel(
                    "Auto Portrait Upside Down",
                    Screen.autorotateToPortraitUpsideDown.ToString()
                );
#if UNITY_2017_2_OR_NEWER && !UNITY_2017_2_0
                DrawTagLabel("Safe Area", Screen.safeArea.ToString());
#endif
#if UNITY_2019_2_OR_NEWER
                DrawTagLabel("Cutouts", GetCutoutsString(Screen.cutouts));
#endif
                DrawTagLabel("Support Resolutions", GetResolutionsString(Screen.resolutions));
            }
            GUILayout.EndVertical();
        }

        private string GetSleepTimeoutDescription(int sleepTimeout)
        {
            if (sleepTimeout == SleepTimeout.NeverSleep)
                return "Never Sleep";

            if (sleepTimeout == SleepTimeout.SystemSetting)
                return "System Setting";

            return sleepTimeout.ToString();
        }

        private string GetResolutionString(Resolution resolution)
        {
            return TextUtility.Format(
                "{0} x {1} @ {2}Hz",
                resolution.width.ToString(),
                resolution.height.ToString(),
#if UNITY_6000_0_OR_NEWER
                resolution.refreshRateRatio.ToString()
#else
                resolution.refreshRate.ToString()
#endif
            );
        }

        private string GetCutoutsString(Rect[] cutouts)
        {
            var cutoutStrings = new string[cutouts.Length];
            for (var i = 0; i < cutouts.Length; i++)
                cutoutStrings[i] = cutouts[i].ToString();

            return string.Join("; ", cutoutStrings);
        }

        private string GetResolutionsString(Resolution[] resolutions)
        {
            var resolutionStrings = new string[resolutions.Length];
            for (var i = 0; i < resolutions.Length; i++)
                resolutionStrings[i] = GetResolutionString(resolutions[i]);

            return string.Join("; ", resolutionStrings);
        }
    }
}
