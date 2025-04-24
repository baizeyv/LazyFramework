using Lazy;

namespace Lazy
{
    public class WindowWrapper
    {
        public string WindowName { get; set; }

        public IDebuggerWindow Window { get; set; }
    }
}
