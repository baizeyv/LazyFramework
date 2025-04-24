using Lazy;

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
                    Lazy.Debugger.Instance.SetShowType(DebuggerShowType.Icon);
                    // TODO: Show GUIMASK
                },
                "#c8161d"
            );
            base.OnBeforeDrawScroll();
        }
    }
}
