namespace Lazy.Example
{
    public class TestMonoNode : MonoNode
    {
        internal override void Process()
        {
            Log.MsgD("HELLO");
        }

        internal override void PhysicsProcess() { }

        internal override void LateProcess() { }

        internal override bool AllowUpdate()
        {
            return true;
        }

        internal override bool AllowFixedUpdate()
        {
            return false;
        }

        internal override bool AllowLateUpdate()
        {
            return false;
        }

        internal override int Priority()
        {
            return 1;
        }
    }
}
