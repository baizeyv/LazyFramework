using System;
using Lazy.Debugger.Misc;
using Lazy.Runtime.Utility;
using UnityEngine;

namespace Lazy.Debugger.Module
{
    [Serializable]
    public class SystemWindow : ScrollableDebuggerWindowBase
    {
        protected override void OnBeforeDrawScroll()
        {
            base.OnBeforeDrawScroll();
            GUILayout.Label("<b>System information</b>");
        }

        protected override void OnDrawScrollableWindow()
        {
            GUILayout.BeginVertical("box");
            {
                DrawTagLabel("Device Unique ID", SystemInfo.deviceUniqueIdentifier);
                DrawTagLabel("Device Name", SystemInfo.deviceName);
                DrawTagLabel("Device Type", SystemInfo.deviceType.ToString());
                DrawTagLabel("Device Model", SystemInfo.deviceModel);
                DrawTagLabel("Processor Type", SystemInfo.processorType);
                DrawTagLabel("Processor Count", SystemInfo.processorCount.ToString());
                DrawTagLabel(
                    "Processor Frequency",
                    TextUtility.Format("{0} MHz", SystemInfo.processorFrequency.ToString())
                );
                DrawTagLabel(
                    "System Memory Size",
                    TextUtility.Format("{0} MB", SystemInfo.systemMemorySize.ToString())
                );
#if UNITY_5_5_OR_NEWER
                DrawTagLabel(
                    "Operating System Family",
                    SystemInfo.operatingSystemFamily.ToString()
                );
#endif
                DrawTagLabel("Operating System", SystemInfo.operatingSystem);
#if UNITY_5_6_OR_NEWER
                DrawTagLabel("Battery Status", SystemInfo.batteryStatus.ToString());
                DrawTagLabel("Battery Level", GetBatteryLevelString(SystemInfo.batteryLevel));
#endif
#if UNITY_5_4_OR_NEWER
                DrawTagLabel("Supports Audio", SystemInfo.supportsAudio.ToString());
#endif
                DrawTagLabel(
                    "Supports Location Service",
                    SystemInfo.supportsLocationService.ToString()
                );
                DrawTagLabel("Supports Accelerometer", SystemInfo.supportsAccelerometer.ToString());
                DrawTagLabel("Supports Gyroscope", SystemInfo.supportsGyroscope.ToString());
                DrawTagLabel("Supports Vibration", SystemInfo.supportsVibration.ToString());
                DrawTagLabel("Genuine", Application.genuine.ToString());
                DrawTagLabel(
                    "Genuine Check Available",
                    Application.genuineCheckAvailable.ToString()
                );
            }
            GUILayout.EndVertical();
        }

        private string GetBatteryLevelString(float batteryLevel)
        {
            if (batteryLevel < 0f)
                return "Unavailable";

            return batteryLevel.ToString("P0");
        }
    }
}
