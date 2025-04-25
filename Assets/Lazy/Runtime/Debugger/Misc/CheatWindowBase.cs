namespace Lazy
{
    public abstract class CheatWindowBase : ScrollableDebuggerWindowBase
    {
        protected override void OnBeforeDrawScroll()
        {
            DrawButton(
                "Close ✖",
                () =>
                {
                    Debugger.Instance.SetShowType(DebuggerShowType.Icon);
                    UIRoot.Instance.debuggerRaycastBlocker.raycastTarget = false;
                },
                "#c8161d"
            );
            base.OnBeforeDrawScroll();
        }
    }
}
