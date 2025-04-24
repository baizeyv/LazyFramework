using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace Lazy
{
    [Serializable]
    public class ProfilerWindow : ScrollableDebuggerWindowBase
    {
        protected override void OnBeforeDrawScroll()
        {
            base.OnBeforeDrawScroll();
            GUILayout.Label("<b>Profiler information</b>");
        }

        protected override void OnDrawScrollableWindow()
        {
            GUILayout.BeginVertical("box");
            {
                DrawTagLabel("Supported", Profiler.supported.ToString());
                DrawTagLabel("Enabled", Profiler.enabled.ToString());
                DrawTagLabel(
                    "Enable Binary Log",
                    Profiler.enableBinaryLog
                        ? TextUtility.Format("True, {0}", Profiler.logFile)
                        : "False"
                );
#if UNITY_2019_3_OR_NEWER
                DrawTagLabel(
                    "Enable Allocation Callstacks",
                    Profiler.enableAllocationCallstacks.ToString()
                );
#endif
#if UNITY_2018_3_OR_NEWER
                DrawTagLabel("Area Count", Profiler.areaCount.ToString());
#endif
#if UNITY_5_3 || UNITY_5_4
                DrawTagLabel(
                    "Max Samples Number Per Frame",
                    Profiler.maxNumberOfSamplesPerFrame.ToString()
                );
#endif
#if UNITY_2018_3_OR_NEWER
                DrawTagLabel("Max Used Memory", GetByteLengthString(Profiler.maxUsedMemory));
#endif
#if UNITY_5_6_OR_NEWER
                DrawTagLabel("Mono Used Size", GetByteLengthString(Profiler.GetMonoUsedSizeLong()));
                DrawTagLabel("Mono Heap Size", GetByteLengthString(Profiler.GetMonoHeapSizeLong()));
                DrawTagLabel("Used Heap Size", GetByteLengthString(Profiler.usedHeapSizeLong));
                DrawTagLabel(
                    "Total Allocated Memory",
                    GetByteLengthString(Profiler.GetTotalAllocatedMemoryLong())
                );
                DrawTagLabel(
                    "Total Reserved Memory",
                    GetByteLengthString(Profiler.GetTotalReservedMemoryLong())
                );
                DrawTagLabel(
                    "Total Unused Reserved Memory",
                    GetByteLengthString(Profiler.GetTotalUnusedReservedMemoryLong())
                );
#else
                DrawTagLabel("Mono Used Size", GetByteLengthString(Profiler.GetMonoUsedSize()));
                DrawTagLabel("Mono Heap Size", GetByteLengthString(Profiler.GetMonoHeapSize()));
                DrawTagLabel("Used Heap Size", GetByteLengthString(Profiler.usedHeapSize));
                DrawTagLabel(
                    "Total Allocated Memory",
                    GetByteLengthString(Profiler.GetTotalAllocatedMemory())
                );
                DrawTagLabel(
                    "Total Reserved Memory",
                    GetByteLengthString(Profiler.GetTotalReservedMemory())
                );
                DrawTagLabel(
                    "Total Unused Reserved Memory",
                    GetByteLengthString(Profiler.GetTotalUnusedReservedMemory())
                );
#endif
#if UNITY_2018_1_OR_NEWER
                DrawTagLabel(
                    "Allocated Memory For Graphics Driver",
                    GetByteLengthString(Profiler.GetAllocatedMemoryForGraphicsDriver())
                );
#endif
#if UNITY_5_5_OR_NEWER
                DrawTagLabel(
                    "Temp Allocator Size",
                    GetByteLengthString(Profiler.GetTempAllocatorSize())
                );
#endif
                // DrawTagLabel(
                //     "Marshal Cached HGlobal Size",
                //     GetByteLengthString(Utility.Marshal.CachedHGlobalSize)
                // );
            }
            GUILayout.EndVertical();
        }
    }
}
