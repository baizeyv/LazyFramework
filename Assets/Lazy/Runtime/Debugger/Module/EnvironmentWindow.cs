using System;
using Lazy;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lazy
{
    [Serializable]
    public class EnvironmentWindow : ScrollableDebuggerWindowBase
    {
        protected override void OnBeforeDrawScroll()
        {
            base.OnBeforeDrawScroll();
            GUILayout.Label("<b>Environment information</b>");
        }

        protected override void OnDrawScrollableWindow()
        {
            GUILayout.BeginVertical("box");
            {
                DrawTagLabel("Product Name", Application.productName);
                DrawTagLabel("Company Name", Application.companyName);
#if UNITY_5_6_OR_NEWER
                DrawTagLabel("Package Name", Application.identifier);
#else
                DrawTagLabel("Package Name", Application.bundleIdentifier);
#endif
                DrawTagLabel("Application Version", Application.version);
                DrawTagLabel("Unity Version", Application.unityVersion);
                DrawTagLabel("Platform", Application.platform.ToString());
                DrawTagLabel("System Language", Application.systemLanguage.ToString());
                DrawTagLabel("Cloud Project ID", Application.cloudProjectId);
#if UNITY_5_6_OR_NEWER
                DrawTagLabel("Build Guid", Application.buildGUID);
#endif
                DrawTagLabel("Target Frame Rate", Application.targetFrameRate.ToString());
                DrawTagLabel("Internet Reachability", Application.internetReachability.ToString());
                DrawTagLabel(
                    "Background Loading Priority",
                    Application.backgroundLoadingPriority.ToString()
                );
                DrawTagLabel("Is Playing", Application.isPlaying.ToString());
#if UNITY_5_5_OR_NEWER
                DrawTagLabel("Splash Screen Is Finished", SplashScreen.isFinished.ToString());
#else
                DrawTagLabel(
                    "Is Showing Splash Screen",
                    Application.isShowingSplashScreen.ToString()
                );
#endif
                DrawTagLabel("Run In Background", Application.runInBackground.ToString());
#if UNITY_5_5_OR_NEWER
                DrawTagLabel("Install Name", Application.installerName);
#endif
                DrawTagLabel("Install Mode", Application.installMode.ToString());
                DrawTagLabel("Sandbox Type", Application.sandboxType.ToString());
                DrawTagLabel("Is Mobile Platform", Application.isMobilePlatform.ToString());
                DrawTagLabel("Is Console Platform", Application.isConsolePlatform.ToString());
                DrawTagLabel("Is Editor", Application.isEditor.ToString());
                DrawTagLabel("Is Debug Build", Debug.isDebugBuild.ToString());
#if UNITY_5_6_OR_NEWER
                DrawTagLabel("Is Focused", Application.isFocused.ToString());
#endif
#if UNITY_2018_2_OR_NEWER
                DrawTagLabel("Is Batch Mode", Application.isBatchMode.ToString());
#endif
#if UNITY_5_3
                DrawTagLabel("Stack Trace Log Type", Application.stackTraceLogType.ToString());
#endif
                var qualityLevelIndex = QualitySettings.GetQualityLevel();
                DrawTagLabel("Quality Level", QualitySettings.names[qualityLevelIndex]);
            }
            GUILayout.EndVertical();
        }
    }
}
