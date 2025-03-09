using UnityEngine;

namespace Lazy.Log
{
    public class _LogExample : MonoBehaviour
    {
        private void Start()
        {
            // Log.Disable();
            var a = "ccc";
            // Log.SetLogLevel(Log.LogLevel.None);
            Log.D(this).Var(nameof(a), a).Tag(this).Sep().Msg("HELLO").Cr().Msg("WORLD").Do();
            Log.I(this).Var(nameof(a), a).Tag(this).Sep().Msg("HELLO").Do();
            Log.E(this).Var(nameof(a), a).Tag(this).Sep().Msg("HELLO").Do();
            Log.W(this).Var(nameof(a), a).Tag(this).Sep().Msg("HELLO").Do();
        }
    }
}