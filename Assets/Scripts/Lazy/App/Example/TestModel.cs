using Lazy.Rx;

namespace Lazy.App.Example
{
    public interface ITestModel : IModel
    {
        public ReactiveVariable<string> Counter { get; }
        public ReactiveVariable<int> Multiple { get; }
    }

    public class TestModel : ABSModel, ITestModel
    {
        protected override void OnSetup()
        {

        }

        public ReactiveVariable<string> Counter { get; } = new("3");
        public ReactiveVariable<int> Multiple { get; } = new(10);
    }
}