namespace Lazy.Example
{
    public class TestApp : ABSApp<TestApp>
    {
        protected override void Setup()
        {
            RegisterModel<ITestModel>(new TestModel());
            RegisterSystem<ITestSystem>(new TestSystem());
        }
    }
}
