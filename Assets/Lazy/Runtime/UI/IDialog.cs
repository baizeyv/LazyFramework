using System;

namespace Lazy
{
    public interface IDialog : IPanel
    {
        Action Callback { get; set; }
    }
}
