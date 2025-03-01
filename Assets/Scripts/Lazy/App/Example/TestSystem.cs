namespace Lazy.App.Example
{
    public interface ITestSystem : ISystem
    {
        public int FindMultiple();
    }

    public class TestSystem : ABSSystem, ITestSystem
    {
        protected override void OnSetup()
        {

        }

        public int FindMultiple()
        {
            // # Do Something
            return this.LazyQuery<MultipleCount, int>();
        }
    }
}