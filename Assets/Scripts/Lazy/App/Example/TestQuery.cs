namespace Lazy.App.Example
{
    public class MultipleCount : ABSQuery<int>
    {
        private ITestModel _model;
        protected override int OnFire()
        {
            _model ??= this.GetModel<ITestModel>();
            return _model.Multiple.Value * 10;
        }
    }
}