namespace Lazy.Rx.Variables
{
    public class IntVariable : ReactiveVariable<int>
    {

        public IntVariable(int value) : base(value) { }

        /// <summary>
        /// * 隐式转换 从 IntVariable 到 int
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static implicit operator int(IntVariable v)
        {
            return v.Value;
        }

    }
}