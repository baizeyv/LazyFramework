using Lazy.Debugger.Module;

namespace Lazy.Debugger.Misc
{
    public class WindowWrapper
    {
        public string WindowName { get; set; }

        public IDebuggerWindow Window { get; set; }
    }
}
